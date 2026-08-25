from pathlib import Path
import pypdfium2 as pdfium

source = Path(r'D:\Backup\D DRIVE\CODEX PROJECTS\SUMMARIZER APP\outputs\demo-presentation-pack\Credit_Review_Committee_Pack.pdf')
target = Path(r'D:\Backup\D DRIVE\CODEX PROJECTS\SUMMARIZER APP\tmp\demo_assets')
document = pdfium.PdfDocument(source)
for index in range(len(document)):
    image = document[index].render(scale=1.8).to_pil()
    image.save(target / f'committee-pack-page-{index + 1}.png')
