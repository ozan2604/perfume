import pandas as pd
from src.preprocessing.cleaner import DataCleaner
from src.preprocessing.imputer import DataImputer

def main():
    print("Starting PerfuMatch Recommendation Engine Data Preprocessing Pipeline...")
    input_file = "data/raw/perfume_dataset.csv" 
    output_file = "data/processed/perfume_dataset_cleaned.csv"
    
    cleaner = DataCleaner()
    imputer = DataImputer()
    
    try:
        # Using latin1 (ISO-8859-1) encoding to handle special accented characters in perfume names
        # and using sep=';' because the Kaggle dataset is semicolon delimited
        df = pd.read_csv(input_file, encoding='latin1', sep=';')
        print(f"Loaded raw data: {df.shape}")
        
        # 1. Clean data
        df = cleaner.clean(df)
        
        # 2. Impute missing values
        df = imputer.impute(df)
        
        # 4. Feature Engineering and Vectorization
        from src.embedding.vectorizer import FeatureEngineeringPipeline
        print("Initializing Feature Engineering Pipeline (D=100)...")
        pipeline = FeatureEngineeringPipeline(d_dimensions=100)
        embeddings = pipeline.fit_transform(df)
        
        # Determine the final D dimension in case it was smaller than 100
        d_dim = embeddings.shape[1]
        
        # 5. Append embeddings as a JSON string column to the DataFrame
        import json
        print("Attaching embeddings to DataFrame...")
        df['embedding_vector'] = [json.dumps(embeddings[i].tolist()) for i in range(len(df))]
        
        # 6. MSSQL Preparation (Cast remaining objects to string)
        for col in df.select_dtypes(include=['object']):
            if col != 'embedding_vector': # Don't touch our JSON string right now if we already processed it
                df[col] = df[col].astype(str)
                df[col] = df[col].replace('nan', '')
            
        print("Saving processed data with embeddings locally...")
        df.to_csv(output_file, index=False)
        print(f"Cleaned dataset saved to {output_file}")
        
        # 7. Upload to MSSQL
        from src.database.uploader import DatabaseUploader
        
        # Raw PyODBC connection string
        conn_str = (
            r'Driver={ODBC Driver 17 for SQL Server};'
            r'Server=DESKTOP-H4P4VAC\SQLEXPRESS;'
            r'Database=PerfumeDb;'
            r'Trusted_Connection=yes;'
        )
        
        uploader = DatabaseUploader(connection_string=conn_str)
        uploader.upload_to_mssql(df, table_name="Perfumes")
    except FileNotFoundError:
        print(f"File {input_file} not found. Please place your raw dataset in the 'data/raw/' folder.")
    except Exception as e:
        print(f"An error occurred: {e}")

if __name__ == "__main__":
    main()
