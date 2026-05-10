import pandas as pd
import pyodbc

def fast_update():
    print("Veriler yükleniyor...")
    csv_path = 'perfumedbwithımageurl.csv'
    df = pd.read_csv(csv_path, sep=';', header=None, on_bad_lines='skip', encoding='latin1')
    
    # Extract only Url and ImageUrl
    df_images = df[[0, 18]].copy()
    df_images.columns = ['Url', 'ImageUrl']
    
    # Drop rows without ImageUrl
    df_images = df_images.dropna(subset=['ImageUrl', 'Url'])
    df_images['Url'] = df_images['Url'].astype(str).str.strip()
    df_images['ImageUrl'] = df_images['ImageUrl'].astype(str).str.strip()
    
    print(f"{len(df_images)} geçerli resim bulundu. Temp tablo oluşturuluyor...")
    
    conn_str = (
        r'Driver={ODBC Driver 17 for SQL Server};'
        r'Server=DESKTOP-H4P4VAC\SQLEXPRESS;'
        r'Database=PerfumeDb;'
        r'Trusted_Connection=yes;'
    )
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    # Create temp table
    cursor.execute('''
        IF OBJECT_ID('TempImages', 'U') IS NOT NULL DROP TABLE TempImages;
        CREATE TABLE TempImages (Url NVARCHAR(MAX), ImageUrl NVARCHAR(MAX));
    ''')
    conn.commit()
    
    print("Temp tabloya veriler yükleniyor (fast_executemany ile)...")
    cursor.fast_executemany = True
    
    # Insert data
    data_to_insert = [tuple(x) for x in df_images.values]
    
    batch_size = 5000
    for i in range(0, len(data_to_insert), batch_size):
        batch = data_to_insert[i:i+batch_size]
        cursor.executemany("INSERT INTO TempImages (Url, ImageUrl) VALUES (?, ?)", batch)
        conn.commit()
        
    print("Veriler temp tabloya aktarıldı. Şimdi asıl tablo tek seferde güncelleniyor...")
    
    cursor.execute('''
        UPDATE p
        SET p.ImageUrl = t.ImageUrl
        FROM Perfumes p
        INNER JOIN TempImages t ON p.Url = t.Url
    ''')
    conn.commit()
    
    cursor.execute('DROP TABLE TempImages')
    conn.commit()
    
    conn.close()
    
    print("Bitti! Tüm veritabanı anında güncellendi.")

if __name__ == "__main__":
    fast_update()
