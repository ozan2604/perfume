import pandas as pd
import numpy as np
from sklearn.preprocessing import OneHotEncoder, MinMaxScaler
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.decomposition import TruncatedSVD
import warnings
warnings.filterwarnings('ignore')

class FeatureEngineeringPipeline:
    def __init__(self, d_dimensions=320):
        self.d_dimensions = d_dimensions
        self.scaler_rating = MinMaxScaler()
        self.scaler_rating_count = MinMaxScaler()
        self.scaler_year = MinMaxScaler()
        self.gender_enc = OneHotEncoder(sparse_output=False, handle_unknown='ignore')
        self.country_enc = OneHotEncoder(sparse_output=False, handle_unknown='ignore')
        self.tfidf = TfidfVectorizer(max_features=800, stop_words='english')
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
        self.brand_freq_ = None

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
            raise ValueError("Accord vocab is not fitted.")

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

    def _combine_notes(self, df: pd.DataFrame) -> list:
        texts = []
        for _, row in df.iterrows():
            top = str(row.get('top_notes', '')) if not pd.isna(row.get('top_notes')) else ''
            mid = str(row.get('middle_notes', '')) if not pd.isna(row.get('middle_notes')) else ''
            base = str(row.get('base_notes', '')) if not pd.isna(row.get('base_notes')) else ''
            
            # Remove commas to let TF-IDF split freely
            top = top.replace(',', ' ')
            mid = mid.replace(',', ' ')
            base = base.replace(',', ' ')
            
            # Base notes added twice to increase their weight/dominance in TF-IDF
            combined = f"{top} {mid} {base} {base}"
            texts.append(combined)
        return texts

    def fit_transform(self, df: pd.DataFrame) -> np.ndarray:
        print("Starting Enhanced Feature Engineering Pipeline...")

        # 0) Fit accord vocabulary
        self._fit_accord_vocab(df)

        # 1) Brand Frequency Encoding
        print("Encoding brand (Frequency Encoding)...")
        brand_col = df['brand'].fillna('Unknown')
        self.brand_freq_ = brand_col.value_counts(normalize=True).to_dict()
        brand_encoded = brand_col.map(self.brand_freq_).fillna(0).values.reshape(-1, 1)

        # 2) Rating scaling
        print("Scaling rating and rating count...")
        rating_col = 'rating_value' if 'rating_value' in df.columns else 'rating'
        
        num_rating = df[[rating_col]].copy()
        num_rating[rating_col] = pd.to_numeric(num_rating[rating_col], errors='coerce').fillna(0)
        rating_scaled = self.scaler_rating.fit_transform(num_rating)

        rating_count_col = 'rating_count' if 'rating_count' in df.columns else 'ratingcount'
        num_count = df[[rating_count_col]].copy() if rating_count_col in df.columns else pd.DataFrame(np.zeros(len(df)), columns=[rating_count_col])
        num_count[rating_count_col] = pd.to_numeric(num_count[rating_count_col], errors='coerce').fillna(0)
        # Apply 0.25 weight coefficient
        rating_count_scaled = self.scaler_rating_count.fit_transform(num_count) * 0.25

        # 3) Gender & Country One-Hot (Multiplying by 0.25 for low impact)
        print("Encoding gender and country (low impact)...")
        gender_col = df[['gender']].fillna('unknown') if 'gender' in df.columns else pd.DataFrame(['unknown']*len(df), columns=['gender'])
        gender_encoded = self.gender_enc.fit_transform(gender_col) * 0.25

        country_col = df[['country']].fillna('unknown') if 'country' in df.columns else pd.DataFrame(['unknown']*len(df), columns=['country'])
        country_encoded = self.country_enc.fit_transform(country_col) * 0.25
        
        # 3.5) Year Scaling
        print("Scaling year...")
        year_col = 'year'
        num_year = df[[year_col]].copy() if year_col in df.columns else pd.DataFrame(np.zeros(len(df)), columns=[year_col])
        num_year[year_col] = pd.to_numeric(num_year[year_col], errors='coerce').fillna(2000)
        year_scaled = self.scaler_year.fit_transform(num_year) * 0.25

        # 4) TF-IDF for Notes
        print("Applying TF-IDF to Top/Middle/Base Notes (Base notes doubled)...")
        notes_texts = self._combine_notes(df)
        notes_tfidf = self.tfidf.fit_transform(notes_texts).toarray()

        # 5) Weighted accords
        print("Building weighted accords...")
        accord_matrix = self._weighted_accord_binarization(df)

        # 6) Concatenate features
        concatenated_features = np.hstack((
            accord_matrix, 
            brand_encoded, 
            rating_scaled, 
            rating_count_scaled, 
            gender_encoded, 
            country_encoded, 
            year_scaled,
            notes_tfidf
        ))
        
        print(f"Raw feature dimension size (before SVD): {concatenated_features.shape[1]}")

        # 7) SVD
        if concatenated_features.shape[1] > self.d_dimensions:
            print(f"Applying TruncatedSVD -> {self.d_dimensions} dims...")
            final_embeddings = self.svd.fit_transform(concatenated_features)
        else:
            print("Feature size <= target dims. Skipping SVD.")
            final_embeddings = concatenated_features

        print(f"Final Embedding Dimension: {final_embeddings.shape}")
        return final_embeddings