from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import KeepTogether, PageBreak, Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle

OUTPUT = r'D:\Backup\D DRIVE\CODEX PROJECTS\SUMMARIZER APP\outputs\demo-presentation-pack\Credit_Review_Committee_Pack.pdf'

NAVY = colors.HexColor('#284661')
GOLD = colors.HexColor('#D99A36')
PALE_GOLD = colors.HexColor('#F4E3C6')
CREAM = colors.HexColor('#FFF9EF')
INK = colors.HexColor('#2E251A')
MUTED = colors.HexColor('#75634D')
LINE = colors.HexColor('#E6D6BD')
RED = colors.HexColor('#A84835')

styles = getSampleStyleSheet()
styles.add(ParagraphStyle(name='PackTitle', parent=styles['Title'], fontName='Helvetica-Bold', fontSize=24, leading=30, textColor=NAVY, alignment=TA_LEFT, spaceAfter=10))
styles.add(ParagraphStyle(name='PackSubtitle', parent=styles['Normal'], fontName='Helvetica', fontSize=11, leading=16, textColor=MUTED, spaceAfter=8))
styles.add(ParagraphStyle(name='Eyebrow', parent=styles['Normal'], fontName='Helvetica-Bold', fontSize=8.5, leading=10, textColor=GOLD, uppercase=True, spaceAfter=7))
styles.add(ParagraphStyle(name='SectionHeading', parent=styles['Heading2'], fontName='Helvetica-Bold', fontSize=14, leading=18, textColor=NAVY, spaceBefore=16, spaceAfter=7))
styles.add(ParagraphStyle(name='BodyCopy', parent=styles['BodyText'], fontName='Helvetica', fontSize=10, leading=15, textColor=INK, spaceAfter=8))
styles.add(ParagraphStyle(name='SmallNote', parent=styles['Normal'], fontName='Helvetica', fontSize=8, leading=11, textColor=MUTED))
styles.add(ParagraphStyle(name='MetricValue', parent=styles['Normal'], fontName='Helvetica-Bold', fontSize=17, leading=21, textColor=NAVY, alignment=TA_CENTER))
styles.add(ParagraphStyle(name='MetricLabel', parent=styles['Normal'], fontName='Helvetica-Bold', fontSize=8, leading=10, textColor=MUTED, alignment=TA_CENTER))

def p(text, style='BodyCopy'):
    return Paragraph(text, styles[style])

def footer(canvas, doc):
    canvas.saveState()
    width, _ = A4
    canvas.setStrokeColor(LINE)
    canvas.line(doc.leftMargin, 0.55 * inch, width - doc.rightMargin, 0.55 * inch)
    canvas.setFont('Helvetica', 8)
    canvas.setFillColor(MUTED)
    canvas.drawString(doc.leftMargin, 0.35 * inch, 'Northstar Finance Group - Fictional Demonstration Data')
    canvas.drawRightString(width - doc.rightMargin, 0.35 * inch, f'Credit Review Committee Pack | Page {doc.page}')
    canvas.restoreState()

doc = SimpleDocTemplate(OUTPUT, pagesize=A4, leftMargin=0.7 * inch, rightMargin=0.7 * inch, topMargin=0.7 * inch, bottomMargin=0.75 * inch)
story = []

story.append(p('CREDIT REVIEW COMMITTEE', 'Eyebrow'))
story.append(p('Meridian Components Ltd.', 'PackTitle'))
story.append(p('Pre-approval credit and operating-risk review', 'PackSubtitle'))

notice = Table([[p('<b>Fictional Demonstration Data</b><br/>All names, figures, exposures, and risk statements in this pack are invented for a product demonstration. They do not describe a real client, financial institution, or transaction.', 'BodyCopy')]], colWidths=[6.9 * inch])
notice.setStyle(TableStyle([
    ('BACKGROUND', (0, 0), (-1, -1), CREAM),
    ('BOX', (0, 0), (-1, -1), 0.8, GOLD),
    ('LEFTPADDING', (0, 0), (-1, -1), 14),
    ('RIGHTPADDING', (0, 0), (-1, -1), 14),
    ('TOPPADDING', (0, 0), (-1, -1), 11),
    ('BOTTOMPADDING', (0, 0), (-1, -1), 8),
]))
story += [notice, Spacer(1, 16)]

story.append(p('Executive decision', 'SectionHeading'))
story.append(p('Recommend <b>conditional approval subject to committee review</b>. The underlying fictional client has an adequate facility structure, but approval should be conditioned on updated financial information, customer concentration mitigants, and evidence of supply-chain contingency planning.'))

metrics = [
    [p('$32.3M', 'MetricValue'), p('2', 'MetricValue'), p('8', 'MetricValue'), p('38.3%', 'MetricValue')],
    [p('Portfolio exposure', 'MetricLabel'), p('Open high-severity risks', 'MetricLabel'), p('Open actions', 'MetricLabel'), p('Projected covenant headroom', 'MetricLabel')],
]
metric_table = Table(metrics, colWidths=[1.7 * inch] * 4, rowHeights=[0.42 * inch, 0.35 * inch])
metric_table.setStyle(TableStyle([
    ('BACKGROUND', (0, 0), (-1, -1), colors.white),
    ('BOX', (0, 0), (-1, -1), 0.7, LINE),
    ('INNERGRID', (0, 0), (-1, -1), 0.5, LINE),
    ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
    ('TOPPADDING', (0, 0), (-1, -1), 6),
    ('BOTTOMPADDING', (0, 0), (-1, -1), 4),
]))
story += [metric_table, Spacer(1, 16)]

story.append(p('Client and facility overview', 'SectionHeading'))
story.append(p('Meridian Components Ltd. is a fictional manufacturer of precision assemblies serving transport, energy, and industrial customers. The proposed financing package combines a working-capital revolver, term facilities, trade finance, an equipment lease, and performance guarantees. The review focus is the resilience of cash flow during the Q4 inventory build and the quality of ongoing financial reporting.'))

overview_data = [
    [p('<b>Review area</b>', 'SmallNote'), p('<b>Current assessment</b>', 'SmallNote')],
    [p('Revenue concentration'), p('Top three customer groups account for 58% of annual revenue. A mitigation plan and evidence of contract renewal are required.')],
    [p('Financial reporting'), p('The August management pack was delivered nine business days late. Updated accounts and a covenant certificate are conditions before approval.')],
    [p('Supply chain'), p('Two critical suppliers are located in one region. Dual-source contingency evidence is required before first drawdown.')],
    [p('Covenant resilience'), p('Projected headroom is 38.3%, above the 15% escalation threshold, but sensitivity should be monitored quarterly.')],
]
overview_table = Table(overview_data, colWidths=[1.75 * inch, 5.05 * inch])
overview_table.setStyle(TableStyle([
    ('BACKGROUND', (0, 0), (-1, 0), PALE_GOLD),
    ('BACKGROUND', (0, 1), (-1, -1), colors.white),
    ('GRID', (0, 0), (-1, -1), 0.45, LINE),
    ('VALIGN', (0, 0), (-1, -1), 'TOP'),
    ('LEFTPADDING', (0, 0), (-1, -1), 8),
    ('RIGHTPADDING', (0, 0), (-1, -1), 8),
    ('TOPPADDING', (0, 0), (-1, -1), 7),
    ('BOTTOMPADDING', (0, 0), (-1, -1), 7),
]))
story.append(overview_table)
story.append(PageBreak())

story.append(p('MATERIAL RISKS AND ACTIONS', 'Eyebrow'))
story.append(p('Risk review and approval conditions', 'PackTitle'))
story.append(p('The following items should be highlighted in the committee discussion and tracked to closure after approval.', 'PackSubtitle'))

risk_data = [
    [p('<b>ID</b>', 'SmallNote'), p('<b>Risk</b>', 'SmallNote'), p('<b>Severity</b>', 'SmallNote'), p('<b>Recommended action</b>', 'SmallNote')],
    [p('R-101'), p('Top three customers represent 58% of annual revenue.'), p('<font color="#A84835"><b>High</b></font>'), p('Obtain concentration mitigation plan and contract renewal evidence.')],
    [p('R-102'), p('August management accounts were submitted nine business days late.'), p('<font color="#A84835"><b>High</b></font>'), p('Require updated accounts and covenant certificate before approval.')],
    [p('R-103'), p('Two critical suppliers are located in the same region.'), p('<b>Medium</b>'), p('Confirm dual-source contingency and safety-stock plan.')],
    [p('R-106'), p('Covenant headroom narrows under a 10% revenue stress.'), p('<b>Medium</b>'), p('Set quarterly monitoring and a cash-conversion trigger.')],
    [p('R-108'), p('Management reporting does not include segmented receivables ageing.'), p('<b>Medium</b>'), p('Request customer-level ageing and dispute analysis.')],
]
risk_table = Table(risk_data, colWidths=[0.6 * inch, 2.35 * inch, 0.75 * inch, 3.1 * inch], repeatRows=1)
risk_table.setStyle(TableStyle([
    ('BACKGROUND', (0, 0), (-1, 0), PALE_GOLD),
    ('BACKGROUND', (0, 1), (-1, -1), colors.white),
    ('GRID', (0, 0), (-1, -1), 0.45, LINE),
    ('VALIGN', (0, 0), (-1, -1), 'TOP'),
    ('LEFTPADDING', (0, 0), (-1, -1), 7),
    ('RIGHTPADDING', (0, 0), (-1, -1), 7),
    ('TOPPADDING', (0, 0), (-1, -1), 7),
    ('BOTTOMPADDING', (0, 0), (-1, -1), 7),
]))
story += [risk_table, Spacer(1, 14)]

story.append(p('Recommended committee conditions', 'SectionHeading'))
conditions = [
    ['1', 'Receive current management accounts and covenant certificate before final credit sign-off.'],
    ['2', 'Document a concentration mitigation plan for the three largest customer groups.'],
    ['3', 'Confirm dual-source supplier resilience and inventory contingency arrangements.'],
    ['4', 'Assign named owners and target dates for every open risk-register action.'],
    ['5', 'Implement quarterly monitoring of covenant headroom, receivables ageing, and working-capital utilisation.'],
]
condition_rows = []
for number, action in conditions:
    condition_rows.append([p(f'<b>{number}</b>', 'BodyCopy'), p(action, 'BodyCopy')])
condition_table = Table(condition_rows, colWidths=[0.35 * inch, 6.45 * inch])
condition_table.setStyle(TableStyle([
    ('BACKGROUND', (0, 0), (0, -1), PALE_GOLD),
    ('BACKGROUND', (1, 0), (1, -1), CREAM),
    ('BOX', (0, 0), (-1, -1), 0.6, LINE),
    ('INNERGRID', (0, 0), (-1, -1), 0.4, LINE),
    ('VALIGN', (0, 0), (-1, -1), 'TOP'),
    ('ALIGN', (0, 0), (0, -1), 'CENTER'),
    ('LEFTPADDING', (0, 0), (-1, -1), 8),
    ('RIGHTPADDING', (0, 0), (-1, -1), 8),
    ('TOPPADDING', (0, 0), (-1, -1), 7),
    ('BOTTOMPADDING', (0, 0), (-1, -1), 7),
]))
story.append(condition_table)
story.append(Spacer(1, 20))
story.append(p('Demo prompt suggestion', 'SectionHeading'))
story.append(p('"Summarize the committee pack, onboarding brief, and risk register. Identify the three most important risks, the approval conditions, and the owners who should take the next actions."'))

doc.build(story, onFirstPage=footer, onLaterPages=footer)
