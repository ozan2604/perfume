import pandas as pd
import numpy as np
from sklearn.preprocessing import OneHotEncoder, MinMaxScaler
from sklearn.decomposition import TruncatedSVD

class FeatureEngineeringPipeline:
    def __init__(self, d_dimensions=256):
        self.d_dimensions = d_dimensions
        self.brand_encoder = OneHotEncoder(sparse_output=False, handle_unknown='ignore')
        self.scaler = MinMaxScaler()
        self.svd = TruncatedSVD(n_components=self.d_dimensions, random_state=42)

        self.accord_weights = {
            'mainaccord1': 1.0,
            'mainaccord2': 0.8,
            'mainaccord3': 0.6,
            'mainaccord4': 0.4,
            'mainaccord5': 0.2
        }

        # learned during fit
        self.accord_list_ = None
        self.accord_to_idx_ = None

    def _fit_accord_vocab(self, df: pd.DataFrame):
        unique_accords = set()
        for col in self.accord_weights.keys():
            if col in df.columns:
                vals = df[col].dropna().astype(str).str.strip().unique()
                unique_accords.update([v for v in vals if v and v.lower() != "nan"])

        self.accord_list_ = sorted(unique_accords)
        self.accord_to_idx_ = {a: i for i, a in enumerate(self.accord_list_)}

    def _weighted_accord_binarization(self, df: pd.DataFrame) -> np.ndarray:
        if self.accord_to_idx_ is None:
            raise ValueError("Accord vocab is not fitted. Call fit_transform first (or add fit/transform separation).")

        num_perfumes = len(df)
        num_accords = len(self.accord_list_)
        accord_matrix = np.zeros((num_perfumes, num_accords), dtype=np.float32)

        for i, row in enumerate(df.itertuples(index=False)):
            row_dict = row._asdict()
            for col, weight in self.accord_weights.items():
                if col not in row_dict:
                    continue

                val = row_dict[col]
                if pd.isna(val):
                    continue

                val = str(val).strip()
                if val == "" or val.lower() == "nan":
                    continue

                idx = self.accord_to_idx_.get(val)
                if idx is not None:
                    accord_matrix[i, idx] = max(accord_matrix[i, idx], weight)

        return accord_matrix

    def fit_transform(self, df: pd.DataFrame) -> np.ndarray:
        print("Starting Feature Engineering Pipeline...")

        # 0) Fit accord vocabulary
        self._fit_accord_vocab(df)

        # 1) Brand one-hot
        print("Encoding brand...")
        brand_encoded = self.brand_encoder.fit_transform(df[['brand']].fillna('Unknown'))

        # 2) Rating scaling
        print("Scaling rating...")
        rating_col = 'rating_value' if 'rating_value' in df.columns else 'rating'
        num_df = df[[rating_col]].copy()
        num_df[rating_col] = pd.to_numeric(num_df[rating_col], errors='coerce').fillna(0)
        numerical_scaled = self.scaler.fit_transform(num_df)

        # 3) Weighted accords
        print("Building weighted accords...")
        accord_matrix = self._weighted_accord_binarization(df)

        # 4) Concatenate  -->  gender , year, country bunlar hep filtreleme kısmında ele alınıcak.
        concatenated_features = np.hstack((accord_matrix, brand_encoded, numerical_scaled))
        print(f"Raw feature dimension size (before SVD): {concatenated_features.shape[1]}")

        # 5) SVD
        if concatenated_features.shape[1] > self.d_dimensions:
            print(f"Applying TruncatedSVD -> {self.d_dimensions} dims...")
            final_embeddings = self.svd.fit_transform(concatenated_features)
        else:
            print("Feature size <= target dims. Skipping SVD.")
            final_embeddings = concatenated_features

        print(f"Final Embedding Dimension: {final_embeddings.shape}")
        return final_embeddings