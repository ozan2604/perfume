import pandas as pd
import pyodbc
import sys

def update_perfume_images():
    print("Veritabanına bağlanılıyor...", flush=True)
    conn_str = (
        r'Driver={ODBC Driver 17 for SQL Server};'
        r'Server=DESKTOP-H4P4VAC\SQLEXPRESS;'
        r'Database=PerfumeDb;'
        r'Trusted_Connection=yes;'
    )
    
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    csv_path = 'perfumedbwithımageurl.csv'
    df = pd.read_csv(csv_path, sep=';', header=None, on_bad_lines='skip', encoding='latin1')
    
    print(f"{len(df)} satır CSV'den okundu. Eşleştirmeler yapılıyor...", flush=True)
    
    updates = []
    for idx, row in df.iterrows():
        url = str(row[0]).strip()
        image_url = str(row[18]).strip()
        
        if url and image_url and image_url != 'nan' and url != 'nan':
            updates.append((image_url, url))
            
    print(f"Toplam {len(updates)} güncelleme veritabanına gönderiliyor...", flush=True)
    
    if updates:
        batch_size = 500
        total_updated = 0
        for i in range(0, len(updates), batch_size):
            batch = updates[i:i+batch_size]
            cursor.executemany(
                "UPDATE Perfumes SET ImageUrl = ? WHERE Url = ?",
                batch
            )
            conn.commit()
            total_updated += len(batch)
            print(f"{total_updated} / {len(updates)} işlendi...", flush=True)
    
    conn.close()
    
    print(f"Bitti! İşlem tamamlandı.", flush=True)

if __name__ == "__main__":
    update_perfume_images()
