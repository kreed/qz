all:
	gmcs -o+ -r:System.Windows.Forms -r:System.Drawing -t:winexe Qz.cs
debug:
	gmcs -debug+ -r:System.Windows.Forms -r:System.Drawing Qz.cs
