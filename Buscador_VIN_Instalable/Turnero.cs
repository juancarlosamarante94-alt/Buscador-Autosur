namespace AutosurBuscador {
using System; using System.Collections.Generic; using System.Drawing; using System.Drawing.Printing; using System.IO; using System.Linq; using System.Text; using System.Text.RegularExpressions; using System.Windows.Forms;
using UglyToad.PdfPig; using UglyToad.PdfPig.Content;


static class TurneroTheme {
 public static readonly Color Ink=Color.FromArgb(24,36,53),Muted=Color.FromArgb(107,122,143),Bg=Color.FromArgb(243,246,250),Line=Color.FromArgb(228,234,241),Yellow=Color.FromArgb(255,207,25);
 public static void Button(Button b,bool primary){b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=primary?Yellow:Line;b.FlatAppearance.BorderSize=1;b.BackColor=primary?Yellow:Color.White;b.ForeColor=Ink;b.Font=new Font("Segoe UI",9.5f,FontStyle.Bold);b.Cursor=Cursors.Hand;b.Height=34;b.MinimumSize=new Size(0,34);b.TextAlign=ContentAlignment.MiddleCenter;b.ImageAlign=ContentAlignment.MiddleCenter;b.Padding=new Padding(8,0,8,0);b.UseVisualStyleBackColor=false;}
 public static void Grid(DataGridView g){g.BackgroundColor=Color.White;g.BorderStyle=BorderStyle.None;g.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;g.GridColor=Line;g.EnableHeadersVisualStyles=false;g.RowHeadersVisible=false;g.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;g.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;g.ColumnHeadersHeight=30;g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(246,248,251),ForeColor=Muted,Font=new Font("Segoe UI",8.5f),Padding=new Padding(4,0,0,0)};g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.White,ForeColor=Ink,SelectionBackColor=Color.FromArgb(233,243,255),SelectionForeColor=Ink,Font=new Font("Segoe UI",9),Padding=new Padding(4,0,0,0)};g.RowTemplate.Height=32;g.AllowUserToResizeRows=false;g.ClearSelection();}
}

class AgendaWord { public string Text; public double X,Y; }
class AgendaLine { public double Y; public List<AgendaWord> Words=new List<AgendaWord>(); public string Text { get { Words.Sort(delegate(AgendaWord a,AgendaWord b){return a.X.CompareTo(b.X);}); return String.Join(" ",Words.Select(x=>x.Text).ToArray()); } } }
class Appointment { public string Fecha="",Hora="",Cliente="",Telefono="",Modelo="",Patente="",Chasis="",Motor="",Servicio=""; }


public class TurneroPreviewWindow:Form {
 readonly List<Bitmap> pages;readonly Panel viewport=new Panel();readonly PictureBox picture=new PictureBox();readonly Label pageLabel=new Label();readonly ComboBox zoom=new ComboBox();readonly Button previous=new Button(),next=new Button();int index;
 public TurneroPreviewWindow(List<Bitmap> images,Action print){
 pages=images;Text="Vista previa del turnero";StartPosition=FormStartPosition.CenterParent;Size=new Size(1120,850);MinimumSize=new Size(900,500);BackColor=TurneroTheme.Bg;Font=new Font("Segoe UI",10);KeyPreview=true;
 var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=new Padding(0)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,56));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(layout);
 var toolbar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(12,10,12,8),Margin=new Padding(0),BackColor=Color.White};layout.Controls.Add(toolbar,0,0);
 var close=new Button{Text="Cerrar vista previa",Width=162};previous.Text="Anterior";previous.Width=96;next.Text="Siguiente";next.Width=96;var printButton=new Button{Text="Imprimir",Width=96};
 foreach(var button in new[]{close,previous,next,printButton}){TurneroTheme.Button(button,button==close);button.Margin=new Padding(0,0,8,0);toolbar.Controls.Add(button);}
 pageLabel.AutoSize=false;pageLabel.Size=new Size(112,34);pageLabel.TextAlign=ContentAlignment.MiddleCenter;pageLabel.ForeColor=TurneroTheme.Muted;pageLabel.Margin=new Padding(0,0,8,0);toolbar.Controls.Add(pageLabel);
 zoom.DropDownStyle=ComboBoxStyle.DropDownList;zoom.Width=154;zoom.Items.AddRange(new object[]{"Ajustar al ancho","50 %","75 %","100 %","125 %"});zoom.Margin=new Padding(0,4,0,0);toolbar.Controls.Add(zoom);
 viewport.Dock=DockStyle.Fill;viewport.AutoScroll=true;viewport.BackColor=Color.FromArgb(216,223,232);viewport.Margin=new Padding(0);viewport.Controls.Add(picture);picture.Location=new Point(16,16);picture.SizeMode=PictureBoxSizeMode.StretchImage;picture.BackColor=Color.White;layout.Controls.Add(viewport,0,1);
 close.Click+=delegate{Close();};previous.Click+=delegate{if(index>0){index--;RenderPage();}};next.Click+=delegate{if(index+1<pages.Count){index++;RenderPage();}};printButton.Click+=delegate{if(print!=null)print();};zoom.SelectedIndexChanged+=delegate{RenderPage();};viewport.Resize+=delegate{if(zoom.SelectedIndex==0)RenderPage();};zoom.SelectedIndex=0;RenderPage();
 }
 void RenderPage(){if(pages.Count==0)return;picture.Image=pages[index];float factor=zoom.SelectedIndex==0?Math.Max(.2f,Math.Min(1.5f,(viewport.ClientSize.Width-52f)/pages[index].Width)):zoom.SelectedIndex==1?.5f:zoom.SelectedIndex==2?.75f:zoom.SelectedIndex==3?1f:1.25f;picture.Size=new Size((int)(pages[index].Width*factor),(int)(pages[index].Height*factor));viewport.AutoScrollPosition=Point.Empty;viewport.AutoScrollMinSize=new Size(picture.Width+32,picture.Height+32);pageLabel.Text="Página "+(index+1)+" / "+pages.Count;previous.Enabled=index>0;next.Enabled=index+1<pages.Count;}
 protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==Keys.Escape){Close();return true;}return base.ProcessCmdKey(ref msg,keyData);}
 protected override void Dispose(bool disposing){if(disposing){picture.Image=null;foreach(var page in pages)page.Dispose();pages.Clear();}base.Dispose(disposing);}
}

public class TurneroPage:UserControl {
 List<Appointment> turns=new List<Appointment>(); DataGridView grid; Label status=new Label(); NumericUpDown rowHeight=new NumericUpDown();Button previewButton,printButton,exportButton; PrintDocument printer=new PrintDocument(); int printIndex;TurneroPreviewWindow previewWindow;
 public TurneroPage(){Dock=DockStyle.Fill;Padding=new Padding(14);BackColor=Color.FromArgb(243,245,247);BuildUi();printer.DefaultPageSettings.Landscape=false;printer.DefaultPageSettings.Margins=new Margins(30,30,30,30);SetA4();printer.PrintPage+=PrintPage;printer.BeginPrint+=delegate{printIndex=0;};}
 void BuildUi(){
 var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=new Padding(0),Padding=new Padding(0)};
 layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,54));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(layout);
 var top=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,BackColor=Color.White,Padding=new Padding(8,8,8,6),Margin=new Padding(0)};
 var load=new Button{Text="Cargar PDF del turnero",Width=182};previewButton=new Button{Text="Vista previa",Width=115,Enabled=false};printButton=new Button{Text="Imprimir turnero",Width=145,Enabled=false};exportButton=new Button{Text="Exportar PDF",Width=130,Enabled=false};
 foreach(var button in new[]{load,previewButton,printButton,exportButton}){TurneroTheme.Button(button,button==load);button.Margin=new Padding(0,0,8,0);top.Controls.Add(button);}
 top.Controls.Add(new Label{Text="Alto de turno (mm)",AutoSize=true,ForeColor=TurneroTheme.Muted,Margin=new Padding(8,8,8,0),Font=new Font("Segoe UI",9)});
 rowHeight.Minimum=28;rowHeight.Maximum=60;rowHeight.Increment=2;rowHeight.Value=32;rowHeight.Width=64;rowHeight.TextAlign=HorizontalAlignment.Center;rowHeight.Margin=new Padding(0,5,0,0);top.Controls.Add(rowHeight);
 layout.Controls.Add(top,0,0);
 status.Text="Cargá el PDF de la agenda. Se conservarán solamente los turnos Renault.\nUsá las barras para desplazarte; las flechas y Re Pág / Av Pág también permiten navegar.";
 status.Dock=DockStyle.Fill;status.Margin=new Padding(0);status.Padding=new Padding(0,5,0,0);status.Font=new Font("Segoe UI",9);status.ForeColor=TurneroTheme.Muted;layout.Controls.Add(status,0,1);
 string[] names={"Fecha","Hora","Cliente","Teléfono","Modelo Renault","Patente","Chasis","Motor","Servicio"};int[] widths={90,70,220,130,260,100,175,80,430};grid=Tools.Grid(names,widths);TurneroTheme.Grid(grid);grid.ScrollBars=ScrollBars.Both;grid.ColumnHeadersHeight=36;grid.RowTemplate.Height=36;grid.AllowUserToResizeColumns=true;grid.TabStop=true;grid.Columns[0].Frozen=true;grid.Columns[1].Frozen=true;grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(247,249,252);grid.Margin=new Padding(0);grid.CellDoubleClick+=delegate(object o,DataGridViewCellEventArgs e){Tools.Copy(grid,status,e);};layout.Controls.Add(grid,0,2);
 load.Click+=delegate{LoadPdf();};previewButton.Click+=delegate{ShowPreview();};printButton.Click+=delegate{PrintNow();};exportButton.Click+=delegate{ExportPdf();};
 }
 void LoadPdf(){using(var dialog=new OpenFileDialog{Filter="Archivos PDF (*.pdf)|*.pdf",Title="Seleccionar turnero nuevo"}){if(dialog.ShowDialog()!=DialogResult.OK)return;try{Cursor=Cursors.WaitCursor;status.Text="Leyendo y filtrando turnos Renault...";Application.DoEvents();if(previewWindow!=null&&!previewWindow.IsDisposed)previewWindow.Close();turns=Extract(dialog.FileName);CompleteMotors();ShowRows();status.Text=turns.Count+" turno(s) Renault. Se excluyeron Nissan y horarios disponibles.\nDesplazate con las barras o el teclado. Fecha y hora permanecen visibles al mover las columnas.";}catch(Exception ex){turns.Clear();ShowRows();MessageBox.Show("No se pudo leer el PDF.\n\n"+ex.Message,"Turnero",MessageBoxButtons.OK,MessageBoxIcon.Error);status.Text="No se pudo procesar el archivo.";}finally{Cursor=Cursors.Default;}}}

 class AgendaColumns {public double Turn,Advisor,Client,Vehicle,Service,Action,Header,Width;}
 class AgendaRow {public string Time="";public List<AgendaWord> Client=new List<AgendaWord>(),Vehicle=new List<AgendaWord>(),Service=new List<AgendaWord>();}
 static string LabelKey(string value){var text=new StringBuilder();foreach(char c in value.Normalize(NormalizationForm.FormD))if(System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)!=System.Globalization.UnicodeCategory.NonSpacingMark&&Char.IsLetter(c))text.Append(Char.ToUpperInvariant(c));return text.ToString();}
 // Action headings and their button captions can be centered at different X
 // coordinates. Use the leftmost action control to exclude the whole column,
 // including unrecognized text beneath it, before assembling service lines.
 double ActionBoundary(List<AgendaWord> words,double service,double heading){
  var label=words.Where(w=>LabelKey(w.Text)=="ACCION"&&w.X>service).OrderBy(w=>w.Y).FirstOrDefault();
  double limit=label==null?heading:Math.Min(heading,label.X-4);
  double searchFrom=service+Math.Max(20,(limit-service)*.45);
  foreach(var word in words.Where(w=>w.X>=searchFrom)){
   string key=LabelKey(word.Text);
   bool control=key=="AVISAR"||key=="REPROGRAMAR"||key=="CANCELAR"||key=="EDITAR"||
    key.StartsWith("AVISARAUTOLISTO")||key.StartsWith("REPROGRAMARTURNO")||
    key.StartsWith("CANCELARTURNO")||key.StartsWith("EDITARTURNO")||
    key.StartsWith("VERPREORDEN")||key.StartsWith("CAMBIARADISPONIBLE");
   if(key=="VER")control=words.Any(w=>Math.Abs(w.Y-word.Y)<20&&w.X>=word.X-2&&w.X<word.X+75&&LabelKey(w.Text).StartsWith("PREORDEN"));
   if(key=="CAMBIAR")control=words.Any(w=>Math.Abs(w.Y-word.Y)<20&&Math.Abs(w.X-word.X)<90&&LabelKey(w.Text)=="DISPONIBLE");
   if(control)limit=Math.Min(limit,word.X-2);
  }
  return limit;
 }

 AgendaColumns HeaderColumns(List<AgendaWord> words,double width){foreach(var candidate in words.Where(w=>LabelKey(w.Text)=="CLIENTE").OrderBy(w=>w.Y)){var line=words.Where(w=>Math.Abs(w.Y-candidate.Y)<6).OrderBy(w=>w.X).ToList();Func<string,AgendaWord> label=key=>line.FirstOrDefault(w=>LabelKey(w.Text)==key);var turn=label("TURNO");var advisor=label("ASESOR");var vehicle=label("VEHICULO");var service=label("SERVICIOS");var action=label("ACCION");if(turn==null||advisor==null||vehicle==null||service==null||action==null)continue;if(!(turn.X<advisor.X&&advisor.X<candidate.X&&candidate.X<vehicle.X&&vehicle.X<service.X&&service.X<action.X))continue;return new AgendaColumns{Turn=turn.X-4,Advisor=advisor.X-4,Client=candidate.X-4,Vehicle=vehicle.X-4,Service=service.X-4,Action=ActionBoundary(words,service.X-4,action.X-4),Header=line.Where(w=>new[]{"TURNO","ASESOR","CLIENTE","VEHICULO","SERVICIOS","ACCION"}.Contains(LabelKey(w.Text))).Max(w=>w.Y)+7,Width=width};}return null;}
 void AddRowWords(AgendaRow row,IEnumerable<AgendaWord> words,AgendaColumns c){foreach(var w in words){if(w.X>=c.Client&&w.X<c.Vehicle)row.Client.Add(w);else if(w.X>=c.Vehicle&&w.X<c.Service)row.Vehicle.Add(w);else if(w.X>=c.Service&&w.X<c.Action)row.Service.Add(w);}}

 List<AgendaWord> ReadWords(Page page,bool letters){
  if(!letters)return page.GetWords().Select(w=>new AgendaWord{Text=w.Text,X=w.BoundingBox.Left,Y=page.Height-w.BoundingBox.Top}).ToList();
  // Reconstruct words from individual glyphs when PDF word grouping fails.
  var result=new List<AgendaWord>();
  foreach(var line in page.Letters.GroupBy(l=>Math.Round(l.StartBaseLine.Y,1)).OrderByDescending(g=>g.Key)){
   var text=new StringBuilder();double left=0,right=0,top=0;
   foreach(var letter in line.OrderBy(l=>l.StartBaseLine.X)){
    string value=letter.Value;double x=letter.StartBaseLine.X;
    if(String.IsNullOrWhiteSpace(value)||(text.Length>0&&x-right>Math.Max(1.2,letter.GlyphRectangle.Height*.25))){
     if(text.Length>0){result.Add(new AgendaWord{Text=text.ToString(),X=left,Y=page.Height-top});text.Clear();}
    }
    if(String.IsNullOrWhiteSpace(value))continue;
    if(text.Length==0){left=x;top=letter.GlyphRectangle.Top;}
    else top=Math.Max(top,letter.GlyphRectangle.Top);
    text.Append(value);right=letter.EndBaseLine.X;
   }
   if(text.Length>0)result.Add(new AgendaWord{Text=text.ToString(),X=left,Y=page.Height-top});
  }
  return result;
 }

 List<Appointment> ExtractAdaptive(string path){return ExtractAdaptiveWords(path,false);}
 List<Appointment> ExtractAdaptiveWords(string path,bool letters){var rows=new List<AgendaRow>();string date="";AgendaColumns previous=null;double offset=0;using(PdfDocument doc=PdfDocument.Open(path)){int pageNo=0;foreach(Page page in doc.GetPages()){pageNo++;var words=ReadWords(page,letters);if(pageNo==1)date=FindDate(words);var columns=HeaderColumns(words,page.Width);if(columns==null){if(previous==null)throw new Exception("No se detectaron las columnas Turno, Asesor, Cliente, Vehículo, Servicios y Acción en la página "+pageNo+".");double scale=page.Width/previous.Width;columns=new AgendaColumns{Turn=previous.Turn*scale,Advisor=previous.Advisor*scale,Client=previous.Client*scale,Vehicle=previous.Vehicle*scale,Service=previous.Service*scale,Action=previous.Action*scale,Header=0,Width=page.Width};}previous=columns;double bottom=page.Height-12;var footer=words.Where(w=>w.Y>page.Height*.85&&LabelKey(w.Text)=="CAPACIDAD").OrderBy(w=>w.Y).FirstOrDefault();if(footer!=null)bottom=Math.Min(bottom,footer.Y-9);var body=words.Where(w=>w.Y>=columns.Header&&w.Y<bottom).ToList();var times=body.Where(w=>w.X>=columns.Turn&&w.X<columns.Advisor&&Regex.IsMatch(w.Text,@"^\d{1,2}:\d{2}$")).OrderBy(w=>w.Y).ToList();if(rows.Count>0){double first=times.Count>0?times[0].Y-6:bottom;var leading=body.Where(w=>w.Y<first).Select(w=>new AgendaWord{Text=w.Text,X=w.X,Y=w.Y+offset});AddRowWords(rows[rows.Count-1],leading,columns);}for(int i=0;i<times.Count;i++){double start=times[i].Y-6,end=i+1<times.Count?times[i+1].Y-6:bottom;var row=new AgendaRow{Time=times[i].Text};AddRowWords(row,body.Where(w=>w.Y>=start&&w.Y<end).Select(w=>new AgendaWord{Text=w.Text,X=w.X,Y=w.Y+offset}),columns);rows.Add(row);}offset+=page.Height;}}
 var result=new List<Appointment>();foreach(var row in rows){var appointment=ParseAgendaRow(row,date);if(appointment!=null)result.Add(appointment);}return result;}

 // Compatibility reader: the source agenda does not always align clock cells
 // with the top of the customer/vehicle cells or with the heading's left edge.
 // Keep vehicle anchors independent of the clock/header based reader.
 List<Appointment> Extract(string path){
  List<Appointment> adaptive=null;string adaptiveError="";
  try{adaptive=ExtractAdaptive(path);}catch(Exception ex){adaptiveError=ex.Message;}
  var compatible=ExtractByVehicle(path);
  var result=adaptive!=null&&adaptive.Count>=compatible.Count?adaptive:compatible;
  if(result.Count==0){
   try{var glyphRows=ExtractAdaptiveWords(path,true);if(glyphRows.Count>result.Count)result=glyphRows;}catch{}
   try{var glyphRows=ExtractVehicleWords(path,true);if(glyphRows.Count>result.Count)result=glyphRows;}catch{}
  }
  if(result.Count==0){
   int pages=0,characters=0,brands=0,renaults=0,times=0;
   using(var doc=PdfDocument.Open(path)){foreach(var page in doc.GetPages()){pages++;characters+=page.Letters.Count;var words=ReadWords(page,true);brands+=words.Count(IsBrand);renaults+=words.Count(w=>Regex.IsMatch(w.Text,@"^RENAULT",RegexOptions.IgnoreCase));times+=words.Count(w=>Regex.IsMatch(w.Text,@"^\d{1,2}:\d{2}$"));}}
   if(characters==0)throw new Exception("El PDF no contiene texto legible. Se necesita la agenda guardada directamente como PDF.");
   if(renaults>0||(brands==0&&adaptiveError.Length>0))throw new Exception("No se pudieron separar los turnos de esta agenda. Datos de diagnostico (sin datos personales): paginas="+pages+", caracteres="+characters+", marcas="+brands+", horarios="+times+".");
  }
  return result;
 }
 List<Appointment> ExtractByVehicle(string path){return ExtractVehicleWords(path,false);}
 List<Appointment> ExtractVehicleWords(string path,bool letters){
  var all=new List<AgendaWord>();var actionLimits=new Dictionary<AgendaWord,double>();double offset=0,previousAction=520;string date="";
  using(PdfDocument doc=PdfDocument.Open(path)){int number=0;foreach(Page page in doc.GetPages()){
   number++;var raw=ReadWords(page,letters);
   var anchors=raw.Where(IsBrand).OrderBy(w=>w.X).ToList();
   // Printed content can occupy only part of a landscape/wide PDF page.
   // Measure the content itself instead of assuming it fills A4's width.
   double scale=anchors.Count>0?anchors[anchors.Count/2].X/318d:page.Width/595d;
   if(scale<.1)scale=1;
   var words=raw.Select(w=>new AgendaWord{Text=w.Text,X=w.X/scale,Y=w.Y/scale}).ToList();
   if(number==1)date=FindDate(words);
   var columns=HeaderColumns(words,595);
   double action=ActionBoundary(words,395,columns!=null&&columns.Action>400?columns.Action:previousAction);previousAction=action;
   foreach(var word in words){word.Y+=offset;all.Add(word);if(IsBrand(word))actionLimits[word]=action;}
   offset+=page.Height/scale;
  }}
  var brands=all.Where(IsBrand).OrderBy(w=>w.Y).ToList();var result=new List<Appointment>();
  for(int i=0;i<brands.Count;i++){
   var brand=brands[i];if(!Regex.IsMatch(brand.Text,@"^RENAULT",RegexOptions.IgnoreCase))continue;
   double start=brand.Y-24,end=brand.Y+155;
   if(i+1<brands.Count)end=Math.Min(end,brands[i+1].Y-25);
   var words=all.Where(w=>w.Y>=start&&w.Y<=end).ToList();
   var time=words.Where(w=>w.X<80&&Regex.IsMatch(w.Text,@"^\d{1,2}:\d{2}$")).OrderBy(w=>Math.Abs(w.Y-brand.Y)).FirstOrDefault();
   if(time==null)continue;
   var row=new AgendaRow{Time=time.Text};
   // Original reader started at 185 and discarded customer/phone prefixes.
   double vehicle=Math.Max(300,brand.X-2);
   foreach(var word in words){
    if(word.X>=155&&word.X<vehicle&&LabelKey(word.Text)!="CLIENTE")row.Client.Add(word);
    else if(word.X>=vehicle&&word.X<395)row.Vehicle.Add(word);
    else if(word.X>=395&&word.X<actionLimits[brand]&&word.Y>=brand.Y-8)row.Service.Add(word);
   }
   var appointment=ParseAgendaRow(row,date);if(appointment!=null)result.Add(appointment);
  }
  return result;
 }
 bool IsBrand(AgendaWord word){return Regex.IsMatch(word.Text,@"^(RENAULT|NISSAN)(?:\b|(?=[A-Z]))",RegexOptions.IgnoreCase);}

 List<string> RowLines(IEnumerable<AgendaWord> words){return Lines(words,Double.MinValue,Double.MaxValue,Double.MinValue,Double.MaxValue).Select(x=>x.Text.Trim()).Where(x=>x.Length>0).ToList();}
 bool PhoneLine(string line){if(line.Contains("@")||Regex.IsMatch(line,@"\b\d{2}\s*-\s*\d{8}\s*-\s*\d\b"))return false;string clean=Regex.Replace(line,@"^\s*(tel[eé]fono|tel[.]?|celular|cel[.]?)\s*:?\s*","",RegexOptions.IgnoreCase);if(Regex.IsMatch(clean,@"[^\d\s+()./-]"))return false;int digits=Regex.Replace(clean,@"\D","").Length;return digits>=7&&digits<=18;}
 Appointment ParseAgendaRow(AgendaRow row,string date){var vehicle=RowLines(row.Vehicle);if(!vehicle.Any(s=>Regex.IsMatch(s,@"\bRENAULT",RegexOptions.IgnoreCase))||vehicle.Any(s=>Regex.IsMatch(s,@"\bNISSAN",RegexOptions.IgnoreCase)))return null;var a=new Appointment{Fecha=date,Hora=row.Time};var clients=RowLines(row.Client);a.Telefono=clients.FirstOrDefault(PhoneLine)??"";var names=new List<string>();foreach(var line in clients){string key=LabelKey(line);if(line.Contains("@")||PhoneLine(line)||key.Contains("RECORDAR")||key=="TURNOHECHO"||key=="TURNO"||key=="HECHO")break;if(Regex.IsMatch(line,@"[A-Za-zÁÉÍÓÚÑáéíóúñ]")||Regex.IsMatch(line,@"^\d{2}\s*-\s*\d{8}\s*-\s*\d$"))names.Add(line);}a.Cliente=String.Join(" ",names.ToArray()).Trim();
 bool started=false,modelDone=false;var model=new List<string>();foreach(string line in vehicle){string compact=Regex.Replace(line,@"[^A-Za-z0-9]","").ToUpperInvariant(),key=LabelKey(line);if(IsPlate(compact)){a.Patente=compact;modelDone=true;continue;}if(Regex.IsMatch(compact,@"^[A-HJ-NPR-Z0-9]{17}$",RegexOptions.IgnoreCase)){a.Chasis=compact;modelDone=true;continue;}if(key.Contains("NUMERODECHASIS")||key=="VEHICULO"){modelDone=true;continue;}if(Regex.IsMatch(line,@"^\s*[\d.,]+\s*km",RegexOptions.IgnoreCase))continue;string value=line;if(Regex.IsMatch(value,@"\bRENAULT",RegexOptions.IgnoreCase)){started=true;value=Regex.Replace(value,@"\bRENAULT","",RegexOptions.IgnoreCase).Trim();}if(started&&!modelDone&&value.Length>0)model.Add(value);}a.Modelo=String.Join(" ",model.ToArray()).Trim();a.Servicio=String.Join(" ",RowLines(row.Service).Where(s=>LabelKey(s)!="SERVICIOS").ToArray()).Trim();return a;}
 string FindDate(List<AgendaWord>w){foreach(var l in Lines(w,0,400,-1,200)){string t=l.Text.ToLowerInvariant();if(Regex.IsMatch(t,@"\d{1,2}\s+de\s+[a-záéíóú]+\s+de\s+\d{4}")){var m=Regex.Match(t,@"(\d{1,2})\s+de\s+([a-záéíóú]+)\s+de\s+(\d{4})");var months=new Dictionary<string,int>{{"enero",1},{"febrero",2},{"marzo",3},{"abril",4},{"mayo",5},{"junio",6},{"julio",7},{"agosto",8},{"septiembre",9},{"octubre",10},{"noviembre",11},{"diciembre",12}};int mo;if(m.Success&&months.TryGetValue(m.Groups[2].Value,out mo))return m.Groups[1].Value.PadLeft(2,'0')+"/"+mo.ToString("00")+"/"+m.Groups[3].Value.Substring(2);}}return "";}
 bool IsPlate(string s){return Regex.IsMatch(s,@"^[A-Z]{2}\d{3}[A-Z]{2}$",RegexOptions.IgnoreCase)||Regex.IsMatch(s,@"^[A-Z]{3}\d{3}$",RegexOptions.IgnoreCase);}
 void CompleteMotors(){string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"vehiculos.dat");if(!File.Exists(path))return;var byVin=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(string line in File.ReadLines(path,Encoding.UTF8)){string[] p=line.Split('\t');if(p.Length!=7)continue;string vin=Tools.N(Tools.D(p[0]));string motor=Tools.N(Tools.D(p[6]));if(vin.Length>0&&motor.Length>0&&!byVin.ContainsKey(vin))byVin.Add(vin,motor.Length<=3?motor:motor.Substring(0,3));}foreach(var a in turns){string key=Tools.N(a.Chasis);string motor;if(key.Length>0&&byVin.TryGetValue(key,out motor))a.Motor=motor;}}
 void SetA4(){printer.DefaultPageSettings.Landscape=false;printer.DefaultPageSettings.PaperSize=new PaperSize("A4",827,1169);}
 List<AgendaLine> Lines(IEnumerable<AgendaWord> words,double xmin,double xmax,double ymin,double ymax){var result=new List<AgendaLine>();foreach(var w in words.Where(x=>x.X>=xmin&&x.X<xmax&&x.Y>=ymin&&x.Y<=ymax).OrderBy(x=>x.Y).ThenBy(x=>x.X)){AgendaLine line=result.LastOrDefault();if(line==null||Math.Abs(line.Y-w.Y)>3){line=new AgendaLine{Y=w.Y};result.Add(line);}line.Words.Add(w);}return result;}
 void ShowRows(){grid.Rows.Clear();foreach(var a in turns)grid.Rows.Add(a.Fecha,a.Hora,a.Cliente,a.Telefono,a.Modelo,a.Patente,a.Chasis,a.Motor,a.Servicio);previewButton.Enabled=printButton.Enabled=exportButton.Enabled=turns.Count>0;if(grid.Rows.Count>0){grid.CurrentCell=grid.Rows[0].Cells[0];grid.ClearSelection();}}
 void ShowPreview(){if(turns.Count==0)return;try{if(previewWindow!=null&&!previewWindow.IsDisposed){previewWindow.Close();}var images=PreviewPages();var window=new TurneroPreviewWindow(images,PrintNow);previewWindow=window;window.FormClosed+=delegate{if(previewWindow==window)previewWindow=null;};window.Show(FindForm());}catch(Exception ex){MessageBox.Show(this,"No se pudo generar la vista previa.\n"+ex.Message,"Vista previa",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
 List<Bitmap> PreviewPages(){var images=new List<Bitmap>();int saved=printIndex;try{printIndex=0;bool more;do{int before=printIndex;var bitmap=new Bitmap(827,1169);bitmap.SetResolution(100,100);try{using(var graphics=Graphics.FromImage(bitmap)){graphics.PageUnit=GraphicsUnit.Pixel;graphics.Clear(Color.White);var settings=new PageSettings{PaperSize=new PaperSize("A4",827,1169),Margins=new Margins(30,30,30,30),Landscape=false};var args=new PrintPageEventArgs(graphics,new Rectangle(30,30,767,1109),new Rectangle(0,0,827,1169),settings);PrintPage(null,args);more=args.HasMorePages;}if(more&&printIndex<=before)throw new Exception("No se pudo avanzar al siguiente turno.");images.Add(bitmap);bitmap=null;}finally{if(bitmap!=null)bitmap.Dispose();}if(images.Count>Math.Max(1,turns.Count))throw new Exception("La vista previa excedió las páginas esperadas.");}while(more);return images;}catch{foreach(var image in images)image.Dispose();throw;}finally{printIndex=saved;}}

 void PrintNow(){using(var d=new PrintDialog{Document=printer})if(d.ShowDialog(FindForm())==DialogResult.OK){printIndex=0;printer.Print();}}
 void ExportPdf(){string pdfPrinter="";foreach(string name in PrinterSettings.InstalledPrinters)if(name.Equals("Microsoft Print to PDF",StringComparison.OrdinalIgnoreCase)){pdfPrinter=name;break;}if(pdfPrinter.Length==0){MessageBox.Show("No se encontro la impresora 'Microsoft Print to PDF' de Windows.","Exportar PDF",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}string suggested="Turnos_de_Taller_"+TitleDate().Replace('/','-')+".pdf";using(var save=new SaveFileDialog{Filter="Archivo PDF (*.pdf)|*.pdf",Title="Guardar turnero como PDF",FileName=suggested,DefaultExt="pdf",AddExtension=true}){if(save.ShowDialog()!=DialogResult.OK)return;string oldPrinter=printer.PrinterSettings.PrinterName;bool oldPrintToFile=printer.PrinterSettings.PrintToFile;string oldFile=printer.PrinterSettings.PrintFileName;PrintController oldController=printer.PrintController;try{printer.PrinterSettings.PrinterName=pdfPrinter;printer.PrinterSettings.PrintToFile=true;printer.PrinterSettings.PrintFileName=save.FileName;printer.PrintController=new StandardPrintController();printIndex=0;printer.Print();MessageBox.Show("El turnero se exporto correctamente en:\n"+save.FileName,"PDF guardado",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception ex){MessageBox.Show("No se pudo exportar el PDF.\n\n"+ex.Message,"Exportar PDF",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{try{printer.PrintController=oldController;printer.PrinterSettings.PrintToFile=oldPrintToFile;if(!String.IsNullOrEmpty(oldFile))printer.PrinterSettings.PrintFileName=oldFile;if(!String.IsNullOrEmpty(oldPrinter))printer.PrinterSettings.PrinterName=oldPrinter;}catch{}printIndex=0;}}}
 protected override void Dispose(bool disposing){if(disposing){if(previewWindow!=null&&!previewWindow.IsDisposed)previewWindow.Close();printer.Dispose();}base.Dispose(disposing);}
 string TitleDate(){if(turns.Count==0)return "";string value=turns[0].Fecha.Trim();DateTime parsed;if(DateTime.TryParseExact(value,new[]{"dd/MM/yy","d/M/yy","dd/MM/yyyy","d/M/yyyy"},System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out parsed))return parsed.ToString("dd/MM/yyyy");return value;}
 void PrintPage(object sender,PrintPageEventArgs e){float left=e.MarginBounds.Left,top=e.MarginBounds.Top,width=e.MarginBounds.Width,bottom=e.MarginBounds.Bottom;var titleFont=new Font("Arial",16f,FontStyle.Bold);var normal=new Font("Arial",8f);var bold=new Font("Arial",8f,FontStyle.Bold);var small=new Font("Arial",7f);var format=new StringFormat{Trimming=StringTrimming.Word};string date=TitleDate();string title=date.Length>0?"Turnos de Taller - "+date:"Turnos de Taller";e.Graphics.DrawString(title,titleFont,Brushes.Black,left,top);float y=top+titleFont.GetHeight(e.Graphics)+10;using(var linePen=new Pen(Color.Black,1.5f))e.Graphics.DrawLine(linePen,left,y,left+width,y);y+=4;int pageStart=printIndex;while(printIndex<turns.Count){var a=turns[printIndex];float serviceWidth=width-55;float serviceHeight=e.Graphics.MeasureString(a.Servicio,normal,new SizeF(serviceWidth,1000),format).Height;float itemHeight=Math.Max((float)rowHeight.Value*100f/25.4f,62f+serviceHeight);if(y+itemHeight>bottom&&printIndex>pageStart)break;e.Graphics.DrawLine(Pens.Black,left,y,left+width,y);float xName=left+width*.09f,xVehicle=left+width*.55f,xPlate=left+width*.82f;e.Graphics.DrawString(a.Fecha,small,Brushes.Black,left,y+7);e.Graphics.DrawString(a.Hora,bold,Brushes.Black,left,y+23);e.Graphics.DrawString(a.Cliente,bold,Brushes.Black,new RectangleF(xName,y+6,width*.43f,28),format);e.Graphics.DrawString(a.Telefono,small,Brushes.Black,new RectangleF(xName,y+31,width*.40f,15),format);e.Graphics.DrawString(a.Modelo,bold,Brushes.Black,new RectangleF(xVehicle,y+6,width*.26f,32),format);e.Graphics.DrawString(a.Patente,bold,Brushes.Black,new RectangleF(xPlate,y+6,width*.10f,16),format);if(a.Chasis.Length>0)e.Graphics.DrawString(a.Chasis,small,Brushes.Black,new RectangleF(xVehicle,y+39,width*.38f,14),format);if(a.Motor.Length>0)e.Graphics.DrawString(a.Motor,bold,Brushes.Black,new RectangleF(left+width-62,y+37,34,16),format);e.Graphics.DrawString(a.Servicio,normal,Brushes.Black,new RectangleF(left+12,y+54,serviceWidth,serviceHeight+2),format);e.Graphics.DrawRectangle(Pens.Black,left+width-25,y+9,20,20);y+=itemHeight;printIndex++;}e.Graphics.DrawLine(Pens.Black,left,y,left+width,y);e.HasMorePages=printIndex<turns.Count;if(!e.HasMorePages)printIndex=0;format.Dispose();titleFont.Dispose();normal.Dispose();bold.Dispose();small.Dispose();}
}
}
