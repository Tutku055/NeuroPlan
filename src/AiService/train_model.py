import random
from sklearn.linear_model import LinearRegression
import joblib

# 1. Generate Synthetic Data
# We create fake data so the model can learn the relationship: Complexity / Velocity = Days
X = [] # Inputs: [velocity, remaining_complexity]
y = [] # Target/Output: [days_needed]

for _ in range(1000):
    velocity = random.uniform(1.0, 100.0)
    complexity = random.randint(1, 10)
    
    # The actual result we want the model to predict
    days_needed = complexity / velocity
    
    X.append([velocity, complexity])
    y.append(days_needed)

# 2. Train the Model (Linear Regression)
model = LinearRegression()
model.fit(X, y)

# 3. Save the Trained Model to Disk
joblib.dump(model, 'neuroplan_model.joblib')
print("SUCCESS: Model trained and saved as 'neuroplan_model.joblib'!")