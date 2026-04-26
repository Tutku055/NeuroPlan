import random
from sklearn.ensemble import RandomForestRegressor
import joblib

# 1. Generate Data
X = [] # [velocity, remaining_complexity]
y = [] # [days_needed]

for _ in range(2000):
    # Velocity: Tasks done per day (1 to 20)
    velocity = random.uniform(1.0, 20.0) 
    
    # Complexity: 1 to 10 scale
    complexity = random.randint(1, 10)
    
    # Target: Non-linear relationship
    days_needed = complexity / velocity
    
    X.append([velocity, complexity])
    y.append(days_needed)

# 2. Train Model
model = RandomForestRegressor(n_estimators=100, random_state=42)
model.fit(X, y)

# 3. Save Model
joblib.dump(model, 'neuroplan_model.joblib')
print("SUCCESS: Model trained and saved!")