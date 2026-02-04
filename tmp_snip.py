from pathlib import Path
text = Path('Controllers/RevisiRosterController.cs').read_text(encoding='utf-8')
start = text.index('var response = new')
print(text[start:start+1000])
