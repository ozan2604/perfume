import numpy as np
import json
import pyodbc
from typing import List, Dict, Tuple
import collections
import random

class RecommenderEvaluator:
    """
    Sistemin mevcut Perfume ve UserInteraction verilerini kullanarak
    Öneri sisteminin Precision (Kesinlik), Recall (Kapsama) ve F1-Score
    değerlerini hesaplayan Değerlendirme (Offline Evaluation) sınıfı.
    """
    def __init__(self, connection_string: str):
        self.connection_string = connection_string
        # Key: PerfumeId, Value: numpy array
        self.perfume_vectors: Dict[str, np.ndarray] = {}
        # Key: UserId, Value: List of dicts {'PerfumeId': str, 'IsLiked': bool}
        self.user_interactions: Dict[str, List[dict]] = collections.defaultdict(list)

    def load_data(self):
        conn = pyodbc.connect(self.connection_string)
        cursor = conn.cursor()

        print("Veritabanından Parfüm vektörleri çekiliyor...")
        cursor.execute("SELECT Id, EmbeddingVector FROM Perfumes WHERE EmbeddingVector IS NOT NULL")
        for row in cursor.fetchall():
            perfume_id = str(row[0]).lower()
            try:
                vec = np.array(json.loads(row[1]), dtype=np.float32)
                self.perfume_vectors[perfume_id] = vec
            except:
                pass

        print("Veritabanından Kullanıcı Etkileşimleri çekiliyor...")
        cursor.execute("SELECT UserId, PerfumeId, IsLiked FROM UserPerfumeInteractions")
        for row in cursor.fetchall():
            user_id = str(row[0]).lower()
            perfume_id = str(row[1]).lower()
            if perfume_id in self.perfume_vectors:
                self.user_interactions[user_id].append({
                    'PerfumeId': perfume_id,
                    'IsLiked': bool(row[2])
                })
        
        conn.close()
        print(f"Başarılı! Toplam {len(self.perfume_vectors)} parfüm vektörü ve {len(self.user_interactions)} kullanıcı yüklendi.\n")

    def _cosine_similarity(self, vec_a, vec_b):
        dot = np.dot(vec_a, vec_b)
        norm_a = np.linalg.norm(vec_a)
        norm_b = np.linalg.norm(vec_b)
        if norm_a == 0 or norm_b == 0:
            return 0.0
        return dot / (norm_a * norm_b)

    def evaluate(self, top_k=10, min_liked_interactions=5, penalty_weight=0.5):
        precisions = []
        recalls = []
        f1_scores = []

        valid_users = 0

        for user_id, interactions in self.user_interactions.items():
            liked_perfumes = [i['PerfumeId'] for i in interactions if i['IsLiked']]
            disliked_perfumes = [i['PerfumeId'] for i in interactions if not i['IsLiked']]

            # Model değerlendirmesi yapabilmek için kullanıcının yeterince beğendiği parfüm olması gerek
            if len(liked_perfumes) < min_liked_interactions:
                continue

            valid_users += 1

            # Train/Test Split (Beğenilenlerin %80'i Train, %20'si Test için saklanır)
            random.Random(42).shuffle(liked_perfumes)
            split_idx = int(len(liked_perfumes) * 0.8)
            train_liked = liked_perfumes[:split_idx]
            test_liked = set(liked_perfumes[split_idx:])

            if len(test_liked) == 0:
                continue

            # Disliked öğelerin hepsi train setinde kalsın (öneri yaparken tecrübe olsun diye)
            train_disliked = disliked_perfumes

            # --- USER VECTOR HESAPLAMASI ---
            train_liked_vecs = [self.perfume_vectors[pid] for pid in train_liked]
            train_disliked_vecs = [self.perfume_vectors[pid] for pid in train_disliked]

            user_vector = np.zeros(256, dtype=np.float32)
            if train_liked_vecs:
                user_vector += np.mean(train_liked_vecs, axis=0)
            if train_disliked_vecs:
                user_vector -= np.mean(train_disliked_vecs, axis=0) * penalty_weight

            # --- KOSİNÜS BENZERLİĞİ VE RECOMMENDATION (ÖNERİ OLUŞTURMA) ---
            # Train verisinde gördüğü şeyleri bir daha önermesin (Zaten etkileşimde bulunulmuş)
            train_interacted = set(train_liked).union(set(train_disliked))
            
            candidates = []
            for pid, vec in self.perfume_vectors.items():
                if pid not in train_interacted:
                    sim = self._cosine_similarity(user_vector, vec)
                    candidates.append((pid, sim))

            # En yüksek benzerliği olan K tane aday parfümü seç
            candidates.sort(key=lambda x: x[1], reverse=True)
            top_k_recommendations = set([c[0] for c in candidates[:top_k]])

            # --- METRİKLER (Precision, Recall, F1) ---
            true_positives = len(top_k_recommendations.intersection(test_liked)) # Testtekilerden kaçını doğru bildik?
            
            precision = true_positives / top_k
            recall = true_positives / len(test_liked)
            f1 = 0
            if (precision + recall) > 0:
                f1 = 2 * (precision * recall) / (precision + recall)

            precisions.append(precision)
            recalls.append(recall)
            f1_scores.append(f1)

        if valid_users == 0:
            print(f"Değerlendirme yapılamadı: Yeterli etkileşimi (en az {min_liked_interactions} beğeni) olan kullanıcı bulunamadı.")
            return

        avg_precision = np.mean(precisions)
        avg_recall = np.mean(recalls)
        avg_f1 = np.mean(f1_scores)

        print(f"--- DEĞERLENDİRME SONUÇLARI (Top-{top_k}, Ceza Çarpanı: {penalty_weight}) ---")
        print(f"Değerlendirilen Kullanıcı Sayısı: {valid_users}")
        print(f"Ortalama Precision@{top_k}: {avg_precision:.4f} (Önerilen 10 parfümden ortalama {avg_precision*10:.2f} tanesi tuttu)")
        print(f"Ortalama Recall@{top_k}   : {avg_recall:.4f} (Kullanıcının sevebileceği test parfümlerinin %{avg_recall*100:.1f}'i yakalandı)")
        print(f"Ortalama F1-Score@{top_k}: {avg_f1:.4f}\n")

if __name__ == '__main__':
    CONN_STR = (
        r'Driver={ODBC Driver 17 for SQL Server};'
        r'Server=DESKTOP-H4P4VAC\SQLEXPRESS;'
        r'Database=PerfumeDb;'
        r'Trusted_Connection=yes;'
    )
    
    # 1. DB'den Veri setini çek
    evaluator = RecommenderEvaluator(CONN_STR)
    evaluator.load_data()
    
    # 2. Farklı konfigürasyonlarla sistemi test et ve metrikleri karşılaştır
    # "Dislike ceza çarpanını ne yaparsak algoritmanın başarsı artar?" şeklinde deneyler:
    
    print("[Deney 1: Geniş Ağ (Top-50 Öneri ile)]")
    evaluator.evaluate(top_k=50, min_liked_interactions=5, penalty_weight=0.5)

    print("[Deney 2: Daha Geniş Ağ (Top-200 Öneri ile)]")
    evaluator.evaluate(top_k=200, min_liked_interactions=5, penalty_weight=0.5)

    print("[Deney 3: En Geniş Ağ (Top-500 Öneri ile)]")
    evaluator.evaluate(top_k=500, min_liked_interactions=5, penalty_weight=0.5)
