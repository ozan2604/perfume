import numpy as np
import json
import pyodbc

class UserVectorizer:
    """
    Kullanıcının beğendiği ve beğenmediği parfümleri analiz ederek,
    kullanıcıya özel 100 boyutlu bir tercih vektörü (embedding) oluşturur.
    """
    def __init__(self, connection_string):
        self.connection_string = connection_string

    def update_user_vector(self, user_id):
        """
        Kullanıcının etkileşimlerini veritabanından çeker, 
        yeni vektörü hesaplar ve AspNetUsers (AppUser) tablosuna kaydeder.
        """
        try:
            conn = pyodbc.connect(self.connection_string)
            cursor = conn.cursor()

            # 1. Beğenilen (1) ve Beğenilmeyen (0) parfümlerin vektörlerini çek
            query = """
                SELECT p.EmbeddingVector, i.IsLiked 
                FROM Perfumes p
                JOIN UserPerfumeInteractions i ON p.Id = i.PerfumeId
                WHERE i.UserId = ?
            """
            cursor.execute(query, user_id)
            rows = cursor.fetchall()

            if not rows:
                print(f"User {user_id} has no interactions yet.")
                return False

            liked_vectors = []
            disliked_vectors = []

            for row in rows:
                if row[0]: # EmbeddingVector null değilse
                    vec = np.array(json.loads(row[0]))
                    if row[1] == True: # IsLiked = 1
                        liked_vectors.append(vec)
                    else:              # IsLiked = 0 (Disliked)
                        disliked_vectors.append(vec)

            # 2. Vektör Hesaplama (Ağırlıklı Ortalama)
            # Default olarak 320 boyutlu boş vektör (projenin standart d_dimensions değeri)
            user_vector = np.zeros(320, dtype=np.float32)

            if liked_vectors:
                # Beğenilenlerin ortalamasını al (Pozitif Etki)
                user_vector += np.mean(liked_vectors, axis=0)

            if disliked_vectors:
                # Beğenilmeyenlerin ortalamasını çıkar (Negatif Etki/Ceza)
                # Ceza ağırlığı 0.5 olarak belirlendi (ayarlanabilir)
                user_vector -= np.mean(disliked_vectors, axis=0) * 0.5

            # 3. Sonucu JSON olarak veritabanına kaydet
            vector_json = json.dumps(user_vector.tolist())
            update_query = "UPDATE AspNetUsers SET UserVector = ? WHERE Id = ?"
            cursor.execute(update_query, (vector_json, user_id))
            
            conn.commit()
            conn.close()
            
            print(f"Successfully updated vector for User: {user_id}")
            return True

        except Exception as e:
            print(f"Error in UserVectorizer for {user_id}: {e}")
            return False

if __name__ == "__main__":
    # Test amaçlı veya manuel çalıştırma için
    CONN_STR = (
        r'Driver={ODBC Driver 17 for SQL Server};'
        r'Server=(localdb)\mssqllocaldb;'
        r'Database=PerfumeDb;'
        r'Trusted_Connection=yes;'
    )
    # Örnek bir kullanıcı ID'si ile test edilebilir
    # vectorizer = UserVectorizer(CONN_STR)
    # vectorizer.update_user_vector("KULLANICI_GUID")
