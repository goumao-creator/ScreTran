# marianmt_translate.py
import sys
import torch
from transformers import MarianMTModel, MarianTokenizer

MODEL = "Helsinki-NLP/opus-mt-ja-ru"
tokenizer = MarianTokenizer.from_pretrained(MODEL, local_files_only=True)
model     = MarianMTModel.from_pretrained(MODEL, local_files_only=True)

def translate(text: str) -> str:
    inputs = tokenizer([text], return_tensors="pt", truncation=True)
    with torch.no_grad():
        out = model.generate(**inputs)
    return tokenizer.decode(out[0], skip_special_tokens=True)

if __name__ == "__main__":
    data = sys.stdin.read().strip()
    result = translate(data)
    print(result)
