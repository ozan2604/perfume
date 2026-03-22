import pyodbc
from src.recommendation.user_vectorizer import UserVectorizer

def update_all_users():
    conn_str = (
        r'Driver={ODBC Driver 17 for SQL Server};'
        r'Server=DESKTOP-H4P4VAC\SQLEXPRESS;'
        r'Database=PerfumeDb;'
        r'Trusted_Connection=yes;'
    )
    
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    print("Sistemdeki tüm kullancılar bulunuyor (AspNetUsers Tablosu)...")
    cursor.execute("SELECT Id FROM AspNetUsers")
    user_ids = [row[0] for row in cursor.fetchall()]
    
    conn.close()
    
    print(f"Toplam {len(user_ids)} kullanıcı bulundu. Profil vektörleri 256 Boyuta güncelleniyor...")
    
    vectorizer = UserVectorizer(conn_str)
    basarili = 0
    for uid in user_ids:
        # Update user vector metodu artık src/recommendation/user_vectorizer.py dosyasında '256' kullanıyor.
        if vectorizer.update_user_vector(uid):
            basarili += 1
            
    print(f"\n{basarili}/{len(user_ids)} kullanıcının vektörleri başarıyla 256D mimarisine güncellendi! (Kalan {len(user_ids)-basarili} kullanıcının hiçbir Like/Dislike etkileşimi olmadığı için null bırakıldı.)")

if __name__ == "__main__":
    update_all_users()
