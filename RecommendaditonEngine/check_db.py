import pyodbc
import json

def check_vector_sizes():
    conn_str = (
        r'Driver={ODBC Driver 17 for SQL Server};'
        r'Server=DESKTOP-H4P4VAC\SQLEXPRESS;'
        r'Database=PerfumeDb;'
        r'Trusted_Connection=yes;'
    )

    try:
        conn = pyodbc.connect(conn_str)
        cursor = conn.cursor()
        
        print("=== VERİTABANI VEKTÖR BOYUTU KONTROLÜ ===\n")

        # 1. Parfüm Tablosunu Kontrol Et
        print("--- PARFÜM VEKTÖRÜ (Perfumes Tablosu) ---")
        cursor.execute("SELECT TOP 1 Name, EmbeddingVector FROM Perfumes WHERE EmbeddingVector IS NOT NULL")
        row = cursor.fetchone()
        if row:
            vec = json.loads(row[1])
            print(f"Örnek Parfüm : {row[0]}")
            print(f"Vektör Boyutu: {len(vec)} (Beklenen: 256)")
        else:
            print("Vektörü hesaplanmış parfüm bulunamadı.")
            
        print("\n--- KULLANICI VEKTÖRÜ (AspNetUsers Tablosu) ---")
        cursor.execute("SELECT TOP 1 UserName, UserVector FROM AspNetUsers WHERE UserVector IS NOT NULL")
        row = cursor.fetchone()
        if row:
            vec = json.loads(row[1])
            print(f"Örnek Kullanıcı : {row[0]}")
            print(f"Vektör Boyutu   : {len(vec)} (Beklenen: 256)")
        else:
            print("Vektörü hesaplanmış kullanıcı profili bulunamadı (Herhangi biri etkileşime girmemiş/vektörü sıfırlanmış).")

        conn.close()

    except Exception as e:
        print(f"Veritabanı bağlantı hatası: {e}")

if __name__ == "__main__":
    check_vector_sizes()
