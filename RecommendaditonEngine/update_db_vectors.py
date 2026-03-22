import pandas as pd
import pyodbc
import json
from src.embedding.vectorizer import FeatureEngineeringPipeline

def update_perfume_vectors():
    print("Veritabanına bağlanılıyor ve mevcut parfümler çekiliyor...")
    conn_str = (
        r'Driver={ODBC Driver 17 for SQL Server};'
        r'Server=DESKTOP-H4P4VAC\SQLEXPRESS;'
        r'Database=PerfumeDb;'
        r'Trusted_Connection=yes;'
    )
    
    conn = pyodbc.connect(conn_str)
    
    # 1. DB'den veriyi DataFrame olarak çek
    query = "SELECT * FROM Perfumes"
    df = pd.read_sql(query, conn)
    print(f"{len(df)} parfüm veritabanından çekildi.")
    
    # Pandas ile veritabanındaki kolon isimlerini vectorizer.py'nin beklediği isimlere uyduralım
    df = df.rename(columns={
        'RatingValue': 'rating_value',
        'Brand': 'brand',
        'MainAccord1': 'mainaccord1',
        'MainAccord2': 'mainaccord2',
        'MainAccord3': 'mainaccord3',
        'MainAccord4': 'mainaccord4',
        'MainAccord5': 'mainaccord5'
    })
    
    # 2. Vektörleri yeni 256 boyuta göre hesapla
    print("256 Boyutlu yeni vektörler (Feature Engineering Pipeline) hesaplanıyor...")
    pipeline = FeatureEngineeringPipeline(d_dimensions=256)
    embeddings = pipeline.fit_transform(df)
    
    # 3. Hesaplanan vektörleri DB'ye UPDATE ile yaz (Interactions bozulmasın diye Id sabit kalır)
    print("Yeni vektörler SQL Server'a kaydediliyor (Bu işlem birkaç dakika sürebilir)...")
    cursor = conn.cursor()
    
    update_sql = "UPDATE Perfumes SET EmbeddingVector = ? WHERE Id = ?"
    
    # Batch halinde veya tek tek update. Toplu data için tek tek update yerine executemany daha hızlıdır.
    rows_to_update = []
    for i in range(len(df)):
        vec_json = json.dumps(embeddings[i].tolist())
        perfume_id = str(df.iloc[i]['Id'])
        rows_to_update.append((vec_json, perfume_id))
    
    # 500'erlik batchler ile update işlemi
    batch_size = 500
    for i in range(0, len(rows_to_update), batch_size):
        cursor.executemany(update_sql, rows_to_update[i:i+batch_size])
        conn.commit()
        print(f"Güncellendi: {i + len(rows_to_update[i:i+batch_size])} / {len(rows_to_update)}")
        
    conn.close()
    print("BÜTÜN PARFÜMLER 256 BOYUTA GÜNCELLENDİ!")

if __name__ == "__main__":
    update_perfume_vectors()
