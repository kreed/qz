all:
	gmcs -o+ -r:System.Windows.Forms -r:System.Drawing -t:winexe -resource:words.resources Qz.cs
debug:
	gmcs -debug+ -r:System.Windows.Forms -r:System.Drawing -resource:words.resources Qz.cs
