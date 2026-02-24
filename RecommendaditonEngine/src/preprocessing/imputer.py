import pandas as pd
import numpy as np

class DataImputer:
    def __init__(self):
        pass
        
    def impute(self, df: pd.DataFrame) -> pd.DataFrame:
        print("Imputing missing values...")
        
        year_cols = [col for col in df.columns if 'year' in col or 'date' in col]
        for col in year_cols:
            if df[col].dtype in ['float64', 'int64']:
                median_val = df[col].median()
                df[col] = df[col].fillna(median_val)
                print(f"Filled missing values in '{col}' with median: {median_val}")

        accord_cols = [col for col in df.columns if 'accord' in col or df[col].dtype == 'object']
        for col in accord_cols:
            if df[col].isnull().any():
                mode_val = df[col].mode()[0]
                df[col] = df[col].fillna(mode_val)
                print(f"Filled missing values in '{col}' with mode: {mode_val}")
                
        for col in df.select_dtypes(include=['float64', 'int64']):
            if df[col].isnull().any():
                median_val = df[col].median()
                df[col] = df[col].fillna(median_val)
                print(f"Filled missing values in '{col}' with median: {median_val}")

        return df
