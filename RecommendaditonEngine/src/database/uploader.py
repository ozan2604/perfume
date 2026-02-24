import pandas as pd
import pyodbc
import uuid

class DatabaseUploader:
    def __init__(self, connection_string: str):
        self.connection_string = connection_string

    def upload_to_mssql(self, df: pd.DataFrame, table_name: str = "Perfumes"):
        print(f"Connecting to database via pyodbc and uploading {len(df)} records to '{table_name}'...")
        
        try:
            conn = pyodbc.connect(self.connection_string)
            cursor = conn.cursor()
            
            # Prepare SQL statement
            columns = ['Id', 'Url', 'Name', 'Brand', 'Country', 'Gender', 'RatingValue', 'RatingCount', 'Year',
                       'TopNotes', 'MiddleNotes', 'BaseNotes', 'MainAccord1', 'MainAccord2', 'MainAccord3', 
                       'MainAccord4', 'MainAccord5', 'Perfumer1', 'Perfumer2', 'Perfumer3', 'Perfumer4', 'EmbeddingVector']
                       
            placeholders = ', '.join(['?'] * len(columns))
            sql = f"INSERT INTO {table_name} ({', '.join(columns)}) VALUES ({placeholders})"
            
            batch_size = 500
            for i in range(0, len(df), batch_size):
                batch_df = df.iloc[i:i+batch_size]
                rows = []
                for _, row in batch_df.iterrows():
                    def val(c, is_numeric=False):
                        if c not in row: return None
                        v = row[c]
                        if pd.isna(v) or v == '' or v == 'nan': return None
                        if is_numeric:
                          try: return float(v)
                          except: return None
                        return str(v)
                        
                    uid = str(uuid.uuid4())
                    values = (
                        uid, val('url'), val('perfume'), val('brand'), val('country'), val('gender'),
                        val('rating_value', True), val('rating_count', True), val('year', True),
                        val('top'), val('middle'), val('base'),
                        val('mainaccord1'), val('mainaccord2'), val('mainaccord3'), val('mainaccord4'), val('mainaccord5'),
                        val('perfumer1'), val('perfumer2'), val('perfumer3'), val('perfumer4'),
                        val('embedding_vector')
                    )
                    rows.append(values)
                
                cursor.executemany(sql, rows)
                conn.commit()
                print(f"Uploaded batch {i//batch_size + 1}")
                
            print(f"Successfully uploaded all {len(df)} records to database.")
            cursor.close()
            conn.close()
        except pyodbc.Error as e:
            print(f"PyODBC Error: {e}")
        except Exception as e:
            print(f"Error uploading to database: {e}")
