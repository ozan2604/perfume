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

    def evaluate(self, threshold=0.1, min_total_interactions=10, penalty_weight=0.5):
        precisions = []
        recalls = []
        f1_scores = []

        valid_users = 0

        for user_id, interactions in self.user_interactions.items():
            liked_perfumes = [i['PerfumeId'] for i in interactions if i['IsLiked']]
            disliked_perfumes = [i['PerfumeId'] for i in interactions if not i['IsLiked']]

            # Model değerlendirmesi yapabilmek için kullanıcının yeterince beğendiği parfüm olması gerek
            if (len(liked_perfumes) + len(disliked_perfumes)) < min_total_interactions:
                continue

            valid_users += 1

            # Train/Test Split (Beğenilenlerin %80'i Train, %20'si Test için saklanır)
            random.Random(42).shuffle(liked_perfumes)
            split_idx = int(len(liked_perfumes) * 0.8)
            train_liked = liked_perfumes[:split_idx]
            test_liked = liked_perfumes[split_idx:]

            if len(test_liked) == 0:
                continue

            # Beğenilmeyenlerin de %80'i Train, %20'si Test
            random.Random(42).shuffle(disliked_perfumes)
            dis_split_idx = int(len(disliked_perfumes) * 0.8)
            train_disliked = disliked_perfumes[:dis_split_idx]
            test_disliked = disliked_perfumes[dis_split_idx:]

            # Negatif sampling uygulanmazsa her önümüze gelen parfümü beğenmiş gibi davranır. 
            # Classification probleminde modelin "hayır/beğenmez" demesi gereken test verileri olmalıdır.
            # Eğer test_disliked tamamen boş ise Precision 1 çıkar. Modelin hata payı ölçülemez.
            # Bu durumu çözmek için hiç etkileşime girilmemiş rastgele parfümleri negatif test verisi yapıyoruz.
            if len(test_disliked) == 0:
                interacted = set(liked_perfumes).union(set(disliked_perfumes))
                unseen = [pid for pid in self.perfume_vectors.keys() if pid not in interacted]
                # Test'teki pozitif sayısı kadar negatif (unseen) seçiyoruz
                test_disliked = random.Random(42).sample(unseen, min(len(test_liked), len(unseen)))

            # --- USER VECTOR HESAPLAMASI ---
            train_liked_vecs = [self.perfume_vectors[pid] for pid in train_liked]
            train_disliked_vecs = [self.perfume_vectors[pid] for pid in train_disliked]

            user_vector = np.zeros(256, dtype=np.float32)
            if train_liked_vecs:
                user_vector += np.mean(train_liked_vecs, axis=0)
            if train_disliked_vecs:
                user_vector -= np.mean(train_disliked_vecs, axis=0) * penalty_weight

            # --- KOSİNÜS BENZERLİĞİ VE CLASSIFICATION DEĞERLENDİRMESİ ---
            tp = 0 # True Positive: Gerçekte Beğenildi, Model Beğenildi tahmini yaptı
            fp = 0 # False Positive: Gerçekte Beğenilmedi, Model Beğenildi tahmini yaptı
            tn = 0 # True Negative: Gerçekte Beğenilmedi, Model Beğenilmedi tahmini yaptı
            fn = 0 # False Negative: Gerçekte Beğenildi, Model Beğenilmedi tahmini yaptı

            # Liked Test items
            for pid in test_liked:
                vec = self.perfume_vectors[pid]
                sim = self._cosine_similarity(user_vector, vec)
                if sim >= threshold:
                    tp += 1
                else:
                    fn += 1

            # Disliked Test items
            for pid in test_disliked:
                vec = self.perfume_vectors[pid]
                sim = self._cosine_similarity(user_vector, vec)
                if sim >= threshold:
                    fp += 1
                else:
                    tn += 1

            # Metrik Hesaplamaları
            precision = tp / (tp + fp) if (tp + fp) > 0 else 0
            recall = tp / (tp + fn) if (tp + fn) > 0 else 0
            f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0

            precisions.append(precision)
            recalls.append(recall)
            f1_scores.append(f1)

        if valid_users == 0:
            print(f"Değerlendirme yapılamadı: Yeterli etkileşimi (en az {min_total_interactions} beğeni) olan kullanıcı bulunamadı.")
            return

        avg_precision = np.mean(precisions)
        avg_recall = np.mean(recalls)
        avg_f1 = np.mean(f1_scores)

        print(f"--- SINIFLANDIRMA DEĞERLENDİRMESİ (Threshold: {threshold}, Ceza Çarpanı: {penalty_weight}) ---")
        print(f"Değerlendirilen Kullanıcı Sayısı: {valid_users}")
        print(f"Ortalama Precision (Kesinlik): {avg_precision:.4f} (Önerilen/Like dediklerinin %{avg_precision*100:.1f} i tuttu)")
        print(f"Ortalama Recall (Kapsama): {avg_recall:.4f} (Kullanıcının sevdiklerinin %{avg_recall*100:.1f}'i yakalandı)")
        print(f"Ortalama F1-Score          : {avg_f1:.4f}\n")

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
    
    print("\n[Deney 1: Güvenli Liman (Eski Şampiyon)]")
    evaluator.evaluate(threshold=0.25, min_total_interactions=10, penalty_weight=0.3)

    print("\n[Deney 2: Yüksek Kesinlik (Daha Strict Eşik ve Ceza)]")
    evaluator.evaluate(threshold=0.28, min_total_interactions=10, penalty_weight=0.4)

    print("\n[Deney 3: Keşif Odaklı (Yumuşak Eşik ve Ceza - Yüksek Recall)]")
    evaluator.evaluate(threshold=0.22, min_total_interactions=10, penalty_weight=0.2)
    
    print("\n[Deney 4: Agresif Negatif Öğrenme (Sert Ceza)]")
    evaluator.evaluate(threshold=0.25, min_total_interactions=10, penalty_weight=0.6)
