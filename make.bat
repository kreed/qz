if "%1" == "c"
	gmcs -o+ -r:System.Windows.Forms -r:System.Drawing Qz.cs
else
	gmcs -o+ -r:System.Windows.Forms -r:System.Drawing -t:winexe Qz.cs
