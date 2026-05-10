import pyodbc

def update_perfume_prices():
    print("Veritabanına bağlanılıyor...", flush=True)
    conn_str = (
        r'Driver={ODBC Driver 17 for SQL Server};'
        r'Server=DESKTOP-H4P4VAC\SQLEXPRESS;'
        r'Database=PerfumeDb;'
        r'Trusted_Connection=yes;'
    )
    
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    cursor.execute("SELECT Id, Brand, Country FROM Perfumes")
    perfumes = cursor.fetchall()
    
    print(f"{len(perfumes)} adet parfüm bulundu. Fiyatlar atanıyor...", flush=True)
    
    updates = []
    
    luxury_brands = ['creed', 'amouage', 'roja', 'tom-ford', 'parfums-de-marly', 'xerjoff', 'maison-francis-kurkdjian', 'clive-christian', 'nishane']
    premium_brands = ['dior', 'chanel', 'hermes', 'guerlain', 'ysl', 'yves-saint-laurent', 'georgio-armani', 'armani', 'givenchy', 'prada', 'tommy-hilfiger']
    budget_brands = ['zara', 'avon', 'oriflame', 'antonio-banderas', 'benetton', 'guess', 'david-beckham', 'police']
    
    for row in perfumes:
        p_id = row[0]
        brand = (row[1] or '').lower()
        country = (row[2] or '').lower()
        
        # Sadece sıralama için kullanılacak temsilci fiyatlar
        price = 0
        if any(b in brand for b in luxury_brands):
            price = 350.0  # Luxury
        elif any(b in brand for b in premium_brands):
            price = 135.0  # Premium
        elif any(b in brand for b in budget_brands):
            price = 30.0   # Budget
        else:
            if 'france' in country or 'italy' in country or 'uk' in country:
                price = 100.0  # Default EU
            elif 'uae' in country or 'saudi' in country:
                price = 85.0   # Default Arab
            else:
                price = 70.0   # Default Other
                
        updates.append((price, p_id))
        
    print(f"Veritabanı güncelleniyor...", flush=True)
    
    batch_size = 1000
    total_updated = 0
    for i in range(0, len(updates), batch_size):
        batch = updates[i:i+batch_size]
        cursor.executemany("UPDATE Perfumes SET Price = ? WHERE Id = ?", batch)
        conn.commit()
        total_updated += len(batch)
        print(f"{total_updated} / {len(updates)} fiyat güncellendi...", flush=True)
        
    conn.close()
    print("İşlem tamamlandı!", flush=True)

if __name__ == "__main__":
    update_perfume_prices()
