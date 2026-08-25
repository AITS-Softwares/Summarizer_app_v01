import fs from 'node:fs/promises';
import { SpreadsheetFile, Workbook } from '@oai/artifact-tool';

const outputDir = 'D:/Backup/D DRIVE/CODEX PROJECTS/SUMMARIZER APP/outputs/demo-presentation-pack';
await fs.mkdir(outputDir, { recursive: true });

const wb = Workbook.create();
const dashboard = wb.worksheets.add('Executive Dashboard');
const risks = wb.worksheets.add('Risk Register');
const portfolio = wb.worksheets.add('Portfolio Detail');
const note = wb.worksheets.add('Read Me');
for (const sheet of [dashboard, risks, portfolio, note]) sheet.showGridLines = false;

const navy = '#284661';
const gold = '#D99A36';
const cream = '#FFF9EF';
const paleGold = '#F4E3C6';
const muted = '#8C7B64';
const border = '#E6D6BD';
const white = '#FFFEFA';
const titleFormat = { fill: navy, font: { bold: true, color: '#FFFFFF', size: 16 }, horizontalAlignment: 'left', verticalAlignment: 'center' };
const sectionFormat = { fill: paleGold, font: { bold: true, color: '#5C3E17' }, horizontalAlignment: 'left' };
const headerFormat = { fill: '#F2E8D9', font: { bold: true, color: '#5C4325' }, horizontalAlignment: 'center', verticalAlignment: 'center', wrapText: true };

dashboard.mergeCells('A1:H1');
dashboard.getRange('A1').values = [['Northstar Finance Group - Credit Review Dashboard']];
dashboard.getRange('A1:H1').format = titleFormat;
dashboard.getRange('A1:H1').format.rowHeight = 28;
dashboard.mergeCells('A2:H2');
dashboard.getRange('A2').values = [['Fictional Demonstration Data | Meridian Components Ltd. | Review date: 2026-08-25']];
dashboard.getRange('A2:H2').format = { fill: cream, font: { italic: true, color: muted, size: 10 } };
dashboard.getRange('A4:H4').values = [['Portfolio exposure', 'High-risk items', 'Open actions', 'Average risk score', 'Projected covenant headroom', 'Top 3 customer concentration', 'Reporting status', 'Committee recommendation']];
dashboard.getRange('A4:H4').format = headerFormat;
dashboard.getRange('A5:H5').formulas = [["=SUM('Portfolio Detail'!$E$6:$E$11)", "=COUNTIF('Risk Register'!$H$6:$H$15,\"High\")", "=COUNTIF('Risk Register'!$I$6:$I$15,\"Open\")", "=AVERAGE('Risk Register'!$G$6:$G$15)", "='Portfolio Detail'!$B$14", "='Portfolio Detail'!$H$14", "='Portfolio Detail'!$H$15", "=IF(B5>0,\"Committee review required\",\"Standard approval route\")"]];
dashboard.getRange('A5:H5').format = { fill: white, font: { bold: true, color: '#2E251A', size: 12 }, horizontalAlignment: 'center', verticalAlignment: 'center', wrapText: true, borders: { preset: 'outside', style: 'thin', color: border } };
dashboard.getRange('A5').format.numberFormat = '$#,##0.0,,"M"';
dashboard.getRange('D5:E5').format.numberFormat = '0.0%';
dashboard.getRange('F5').format.numberFormat = '0.0%';
dashboard.getRange('A5:H5').format.rowHeight = 42;
dashboard.mergeCells('A7:H7');
dashboard.getRange('A7').values = [['Executive summary']];
dashboard.getRange('A7:H7').format = sectionFormat;
dashboard.getRange('A8:H10').merge();
dashboard.getRange('A8').values = [["Meridian Components Ltd. is a fictional manufacturing client with a diversified product base and a material working-capital requirement. The review package identifies two high-severity open risks: revenue concentration and delayed financial reporting. Approval should be subject to documented supply-chain contingency evidence, updated management accounts, and a named owner for each open action."]];
dashboard.getRange('A8:H10').format = { fill: cream, font: { color: '#5E503E', size: 11 }, wrapText: true, verticalAlignment: 'top', borders: { preset: 'outside', style: 'thin', color: border } };
dashboard.mergeCells('A12:D12');
dashboard.getRange('A12').values = [['Priority actions']];
dashboard.getRange('A12:D12').format = sectionFormat;
dashboard.getRange('A13:D16').values = [
  ['Priority', 'Required action', 'Owner', 'Target date'],
  ['High', 'Obtain customer concentration mitigation plan', 'Relationship Management', '2026-09-02'],
  ['High', 'Receive August management accounts and covenant certificate', 'Finance', '2026-08-29'],
  ['Medium', 'Confirm dual-source supplier contingency plan', 'Operations', '2026-09-09'],
];
dashboard.getRange('A13:D13').format = headerFormat;
dashboard.getRange('A14:D16').format = { fill: white, borders: { preset: 'inside', style: 'thin', color: '#EFE3D1' }, wrapText: true };
dashboard.getRange('D14:D16').format.numberFormat = 'yyyy-mm-dd';
dashboard.getRange('A13:D16').format.borders = { preset: 'outside', style: 'thin', color: border };
dashboard.getRange('A1:H16').format.columnWidth = 17;
dashboard.getRange('B1:B16').format.columnWidth = 25;
dashboard.getRange('G1:H16').format.columnWidth = 22;
dashboard.freezePanes.freezeRows(4);

risks.mergeCells('A1:J1');
risks.getRange('A1').values = [['Meridian Components Ltd. - Risk Register']];
risks.getRange('A1:J1').format = titleFormat;
risks.mergeCells('A2:J2');
risks.getRange('A2').values = [['Fictional Demonstration Data | Open and closed actions for credit committee review']];
risks.getRange('A2:J2').format = { fill: cream, font: { italic: true, color: muted, size: 10 } };
risks.getRange('A5:J5').values = [['Risk ID', 'Category', 'Risk statement', 'Owner', 'Exposure', 'Opened', 'Risk score', 'Severity', 'Status', 'Recommended action']];
risks.getRange('A5:J5').format = headerFormat;
risks.getRange('A6:J15').values = [
  ['R-101', 'Credit', 'Top three customers represent 58% of annual revenue.', 'Relationship Management', 12400000, '2026-08-12', 0.82, 'High', 'Open', 'Obtain concentration mitigation plan and contract renewal evidence.'],
  ['R-102', 'Financial', 'August management accounts were submitted nine business days late.', 'Finance', 8600000, '2026-08-16', 0.76, 'High', 'Open', 'Require updated accounts and covenant certificate before approval.'],
  ['R-103', 'Operations', 'Two critical suppliers are located in the same region.', 'Operations', 5100000, '2026-08-10', 0.68, 'Medium', 'Open', 'Confirm dual-source contingency and safety-stock plan.'],
  ['R-104', 'Legal', 'Supplier insurance certificates expire within 45 days.', 'Legal', 2400000, '2026-08-18', 0.44, 'Medium', 'Open', 'Collect renewed certificates before first drawdown.'],
  ['R-105', 'Compliance', 'Beneficial ownership evidence has been independently verified.', 'Compliance', 0, '2026-08-08', 0.18, 'Low', 'Closed', 'Retain verification record in onboarding file.'],
  ['R-106', 'Credit', 'Projected covenant headroom narrows under a 10% revenue stress.', 'Credit Risk', 9800000, '2026-08-14', 0.63, 'Medium', 'Open', 'Set quarterly monitoring and cash conversion trigger.'],
  ['R-107', 'Operations', 'Inventory build increases working-capital utilisation before Q4.', 'Operations', 7200000, '2026-08-19', 0.56, 'Medium', 'Open', 'Review inventory ageing during monthly monitoring.'],
  ['R-108', 'Data', 'Management reporting pack does not include a segmented receivables ageing.', 'Finance', 3100000, '2026-08-17', 0.51, 'Medium', 'Open', 'Request customer-level ageing and dispute analysis.'],
  ['R-109', 'Credit', 'Security valuation was completed within the last six months.', 'Credit Risk', 0, '2026-08-09', 0.15, 'Low', 'Closed', 'No further action required.'],
  ['R-110', 'Legal', 'Material contracts include change-of-control clauses.', 'Legal', 1700000, '2026-08-21', 0.37, 'Low', 'Open', 'Confirm clauses do not restrict facility documentation.'],
];
risks.getRange('A6:J15').format = { fill: white, borders: { preset: 'inside', style: 'thin', color: '#F0E5D4' }, wrapText: true, verticalAlignment: 'top' };
risks.getRange('A5:J15').format.borders = { preset: 'outside', style: 'thin', color: border };
risks.getRange('E6:E15').format.numberFormat = '$#,##0';
risks.getRange('F6:F15').format.numberFormat = 'yyyy-mm-dd';
risks.getRange('G6:G15').format.numberFormat = '0%';
risks.getRange('H6:H15').conditionalFormats.add('containsText', { text: 'High', format: { fill: '#FBE4DE', font: { bold: true, color: '#9A3827' } } });
risks.getRange('I6:I15').conditionalFormats.add('containsText', { text: 'Open', format: { fill: '#FFF2D9', font: { color: '#8A5A17' } } });
risks.getRange('A1:J15').format.columnWidth = 14;
risks.getRange('C1:C15').format.columnWidth = 37;
risks.getRange('D1:D15').format.columnWidth = 22;
risks.getRange('J1:J15').format.columnWidth = 44;
risks.getRange('A6:J15').format.rowHeight = 34;
risks.freezePanes.freezeRows(5);

portfolio.mergeCells('A1:H1');
portfolio.getRange('A1').values = [['Meridian Components Ltd. - Portfolio Detail']];
portfolio.getRange('A1:H1').format = titleFormat;
portfolio.mergeCells('A2:H2');
portfolio.getRange('A2').values = [['Fictional Demonstration Data | Facility and financial snapshot used in the credit review']];
portfolio.getRange('A2:H2').format = { fill: cream, font: { italic: true, color: muted, size: 10 } };
portfolio.getRange('A5:H5').values = [['Facility', 'Limit', 'Utilised', 'Availability', 'Outstanding exposure', 'Rate', 'Maturity', 'Comment']];
portfolio.getRange('A5:H5').format = headerFormat;
portfolio.getRange('A6:H11').values = [
  ['Working Capital Revolver', 18000000, 12400000, null, 12400000, 0.0725, '2027-06-30', 'Seasonal inventory and receivables funding'],
  ['Term Loan A', 9500000, 8200000, null, 8200000, 0.0690, '2029-12-31', 'Plant modernisation capex'],
  ['Term Loan B', 6000000, 5900000, null, 5900000, 0.0710, '2030-03-31', 'Acquisition financing'],
  ['Trade Finance Line', 4500000, 2100000, null, 2100000, 0.0740, '2027-09-30', 'Import letters of credit'],
  ['Equipment Lease', 3500000, 2300000, null, 2300000, 0.0615, '2028-05-31', 'Automated assembly equipment'],
  ['Guarantee Facility', 2500000, 1400000, null, 1400000, 0.0580, '2027-12-31', 'Performance guarantees'],
];
portfolio.getRange('D6').formulas = [['=B6-C6']];
portfolio.getRange('D6:D11').fillDown();
portfolio.getRange('A6:H11').format = { fill: white, borders: { preset: 'inside', style: 'thin', color: '#F0E5D4' }, wrapText: true };
portfolio.getRange('A5:H11').format.borders = { preset: 'outside', style: 'thin', color: border };
portfolio.getRange('B6:E11').format.numberFormat = '$#,##0';
portfolio.getRange('F6:F11').format.numberFormat = '0.0%';
portfolio.getRange('G6:G11').format.numberFormat = 'yyyy-mm-dd';
portfolio.getRange('A13:H13').values = [['Key monitoring metrics', 'Value', '', '', '', '', '', '']];
portfolio.getRange('A13:H13').format = sectionFormat;
portfolio.getRange('A14:H15').values = [
  ['Projected covenant headroom', null, '', 'Minimum required headroom', 0.15, '', 'Top 3 customer concentration', null],
  ['Reporting timeliness', 'August pack delivered 9 business days late', '', 'Inventory build outlook', 'Moderate increase expected in Q4', '', 'Recommendation', 'Conditional approval pending actions'],
];
portfolio.getRange('B14').formulas = [['=(D6+D7)/B6']];
portfolio.getRange('H14').formulas = [['=0.58']];
portfolio.getRange('B14').format.numberFormat = '0.0%';
portfolio.getRange('E14').format.numberFormat = '0.0%';
portfolio.getRange('H14').format.numberFormat = '0.0%';
portfolio.getRange('A14:H15').format = { fill: cream, borders: { preset: 'outside', style: 'thin', color: border }, wrapText: true };
portfolio.getRange('A1:H15').format.columnWidth = 20;
portfolio.getRange('H1:H15').format.columnWidth = 30;
portfolio.freezePanes.freezeRows(5);

note.mergeCells('A1:F1');
note.getRange('A1').values = [['Demo data notes']];
note.getRange('A1:F1').format = titleFormat;
note.getRange('A3:F8').values = [
  ['This workbook contains fictional demonstration data only.', '', '', '', '', ''],
  ['Suggested use', 'Attach this workbook with the accompanying PDF and TXT brief, then request an executive summary, risk review, or action tracker.', '', '', '', ''],
  ['Workbook guide', 'Executive Dashboard contains formula-driven summary metrics. Risk Register contains the detailed action-level record. Portfolio Detail contains facility and monitoring data.', '', '', '', ''],
  ['Presentation note', 'All client names, figures, dates, and risk statements are invented for a college demonstration. Do not treat them as financial advice or real customer information.', '', '', '', ''],
  ['Prepared for', 'Document Review Workspace demonstration', '', '', '', ''],
  ['Prepared date', '2026-08-25', '', '', '', ''],
];
note.getRange('A3:F8').format = { fill: cream, wrapText: true, verticalAlignment: 'top', borders: { preset: 'outside', style: 'thin', color: border } };
note.getRange('A1:A8').format.columnWidth = 30;
note.getRange('B1:B8').format.columnWidth = 65;
note.getRange('A3:F8').format.rowHeight = 48;

const check = await wb.inspect({ kind: 'table', range: 'Executive Dashboard!A1:H16', include: 'values,formulas', tableMaxRows: 16, tableMaxCols: 8 });
console.log(check.ndjson);
const errors = await wb.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A', options: { useRegex: true, maxResults: 50 }, summary: 'formula error scan' });
console.log(errors.ndjson);
const preview = await wb.render({ sheetName: 'Executive Dashboard', range: 'A1:H16', scale: 1.5, format: 'png' });
await fs.writeFile(`${outputDir}/Portfolio_Risk_Register_preview.png`, new Uint8Array(await preview.arrayBuffer()));
for (const [sheetName, range, filename] of [
  ['Risk Register', 'A1:J15', 'Risk_Register_preview.png'],
  ['Portfolio Detail', 'A1:H15', 'Portfolio_Detail_preview.png'],
  ['Read Me', 'A1:F8', 'Read_Me_preview.png'],
]) {
  const image = await wb.render({ sheetName, range, scale: 1.25, format: 'png' });
  await fs.writeFile(`${outputDir}/${filename}`, new Uint8Array(await image.arrayBuffer()));
}
const xlsx = await SpreadsheetFile.exportXlsx(wb);
await xlsx.save(`${outputDir}/Portfolio_Risk_Register.xlsx`);
