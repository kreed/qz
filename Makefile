# Is there a good, simple build system for Mono?
#
LIBS=System.Windows.Forms,System.Drawing
RES=words.txt
SRCS=MainWindow.cs Canvas.cs Util.cs Bank.cs Tile.cs Word.cs Meaning.cs
TGT=Qz.exe

CSC=gmcs -r:$(LIBS) -resource:$(RES) $(SRCS) -out:$(TGT)

$(TGT): $(SRCS) $(RES)
	$(CSC) -o+ -t:winexe

.PHONY: debug clean all

debug:
	$(CSC) -debug+

clean:
	rm -f Qz.exe Qz.exe.mdb

all:
	$(CSC) -o+ -t:winexe
