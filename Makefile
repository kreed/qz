# Is there a good, simple build system for Mono?
#
LIBS=System.Windows.Forms,System.Drawing
RES=-resource:words.txt
SRCS=Qz.cs Util.cs Bank.cs Tile.cs Word.cs Meaning.cs

CSC=gmcs -r:$(LIBS) $(RES) $(SRCS)

Qz.exe: $(SRCS)
	$(CSC) -o+ -t:winexe

.PHONY: debug clean all

debug:
	$(CSC) -debug+

clean:
	rm -f Qz.exe Qz.exe.mdb

all:
	$(CSC) -o+ -t:winexe
