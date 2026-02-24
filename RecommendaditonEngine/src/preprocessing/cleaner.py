import pandas as pd
import numpy as np

class DataCleaner:
    def __init__(self):
        pass
        
    def clean(self, df: pd.DataFrame) -> pd.DataFrame:
        print("Cleaning dataset...")
        df.columns = [col.strip().lower().replace(' ', '_') for col in df.columns]
        
        for col in df.select_dtypes(include=['object']):
            df[col] = df[col].apply(
                lambda x: str(x).replace(',', '.') if isinstance(x, str) and ',' in str(x) and any(c.isdigit() for c in str(x)) else x
            )
            
        numeric_candidates = ['rating', 'price', 'capacity']
        for col in numeric_candidates:
            if col in df.columns and df[col].dtype == 'object':
                 df[col] = pd.to_numeric(df[col], errors='coerce')
                 
        return df
