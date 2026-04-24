import uvicorn
from fastapi import FastAPI
from pydantic import BaseModel
from datetime import datetime, timedelta
import joblib
import math

# Load the pre-trained machine learning model
model = None
try:
    model = joblib.load('neuroplan_model.joblib')
    print("SUCCESS: AI Model loaded successfully.")
except FileNotFoundError:
    print("WARNING: Model file 'neuroplan_model.joblib' not found. AI Service will use fallback logic. Please run train_model.py to enable the ML model.")
except Exception as e:
    print(f"ERROR: Could not load model: {e}")

app = FastAPI()

# Define the JSON payload structure expected from the C# backend
class PredictionRequest(BaseModel):
    start_date: str
    velocity: float
    remaining_complexity: int

@app.post("/predict")
async def predict_completion(request: PredictionRequest):
    
    # 1. Predict the number of days required.
    if model:
        prediction = model.predict([[request.velocity, request.remaining_complexity]])
        # Round up since the model might return a fractional day
        days_needed = math.ceil(prediction[0])
    else:
        # Fallback simplistic calculation if model is not loaded
        days_needed = math.ceil(request.remaining_complexity / (request.velocity if request.velocity > 0 else 1.0))
    
    
    # 2. Parse the incoming date string.
    date_str = request.start_date.replace("Z", "+00:00")
    start_date_obj = datetime.fromisoformat(date_str)
    
    # 3. Add the calculated days to the original start date.
    predicted_date_obj = start_date_obj + timedelta(days=days_needed)
    
    # 4. Return the predicted date in the ISO 8601 format expected by C#
    return {
        "predicted_date": predicted_date_obj.strftime("%Y-%m-%dT%H:%M:%SZ")
    }

# Start the server programmatically
if __name__ == "__main__":
    print("Starting AI Service on http://127.0.0.1:8000")
    uvicorn.run(app, host="127.0.0.1", port=8000)