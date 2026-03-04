import pandas as pd
import numpy as np
from sklearn.preprocessing import OneHotEncoder, MinMaxScaler
from sklearn.decomposition import TruncatedSVD

class FeatureEngineeringPipeline:
    def __init__(self, d_dimensions=100):
        self.d_dimensions = d_dimensions
        self.brand_encoder = OneHotEncoder(sparse_output=False, handle_unknown='ignore')
        self.country_encoder = OneHotEncoder(sparse_output=False, handle_unknown='ignore')
        self.gender_encoder = OneHotEncoder(sparse_output=False, handle_unknown='ignore')
        self.scaler = MinMaxScaler()
        self.svd = TruncatedSVD(n_components=self.d_dimensions, random_state=42)
        
        # We define positions and weights for main accords
        self.accord_weights = {
            'mainaccord1': 1.0,
            'mainaccord2': 0.8,
            'mainaccord3': 0.6,
            'mainaccord4': 0.4,
            'mainaccord5': 0.2
        }
        self.unique_accords = set()

    def _weighted_accord_binarization(self, df: pd.DataFrame) -> np.ndarray:
        print("Extracting weighted accords...")
        
        # 1. Find all unique accords
        for col in self.accord_weights.keys():
            if col in df.columns:
                self.unique_accords.update(df[col].dropna().unique())
                
        # Remove empty string or 'nan' from unique accords if present
        self.unique_accords.discard('nan')
        self.unique_accords.discard('')
        
        accord_list = sorted(list(self.unique_accords))
        accord_to_idx = {accord: i for i, accord in enumerate(accord_list)}
        
        # 2. Build the weighted multi-label matrix
        num_perfumes = len(df)
        num_accords = len(accord_list)
        accord_matrix = np.zeros((num_perfumes, num_accords))
        
        for i, row in enumerate(df.itertuples()):
            row_dict = row._asdict()
            for col, weight in self.accord_weights.items():
                if col in row_dict:
                    val = str(row_dict[col])
                    if val != 'nan' and val != '' and val in accord_to_idx:
                        idx = accord_to_idx[val]
                        # We use max in case an accord is mistakenly listed twice, 
                        # though usually position 1 is highest weight
                        accord_matrix[i, idx] = max(accord_matrix[i, idx], weight)
                        
        return accord_matrix

    def fit_transform(self, df: pd.DataFrame) -> np.ndarray:
        print("Starting Feature Engineering Pipeline...")
        
        # 1. Categorical Encodings (Brand, Country, Gender)
        print("Encoding categorical features...")
        brand_encoded = self.brand_encoder.fit_transform(df[['brand']].fillna('Unknown'))
        country_encoded = self.country_encoder.fit_transform(df[['country']].fillna('Unknown'))
        gender_encoded = self.gender_encoder.fit_transform(df[['gender']].fillna('Unknown'))
        
        # 2. Numerical Scaling (Rating, Year)
        print("Scaling numerical features...")
        # using rating_value and year. 
        # Handle if column names are slightly different (e.g. rating_value vs rating)
        rating_col = 'rating_value' if 'rating_value' in df.columns else 'rating'
        
        num_df = df[[rating_col, 'year']].copy()
        num_df[rating_col] = pd.to_numeric(num_df[rating_col], errors='coerce').fillna(0)
        num_df['year'] = pd.to_numeric(num_df['year'], errors='coerce').fillna(num_df['year'].median())
        
        numerical_scaled = self.scaler.fit_transform(num_df)
        
        # 3. Weighted Accords
        accord_matrix = self._weighted_accord_binarization(df)
        
        # 4. Concatenate Blocks
        print("Concatenating feature blocks...")
        concatenated_features = np.hstack((
            accord_matrix,
            brand_encoded,
            country_encoded,
            gender_encoded,
            numerical_scaled
        ))
        
        print(f"Raw feature dimension size (before SVD): {concatenated_features.shape[1]}")
        
        # 5. Dimensionality Reduction to D
        # If the number of columns is less than D, we just return the concatenated features (or padded)
        # But usually brand one-hot makes it > 500
        if concatenated_features.shape[1] > self.d_dimensions:
            print(f"Applying TruncatedSVD to reduce down to {self.d_dimensions} dimensions...")
            final_embeddings = self.svd.fit_transform(concatenated_features)
        else:
            print(f"Feature size <= {self.d_dimensions}. Skipping SVD.")
            final_embeddings = concatenated_features
            
        print(f"Final Embedding Dimension: {final_embeddings.shape}")
        
        return final_embeddings

        # 5. Dimensionality Reduction to D
        # If the number of columns is less than D, we just return the concatenated features (or padded)
        # But usually brand one-hot makes it > 500
        