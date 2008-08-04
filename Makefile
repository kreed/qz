# Is there a good, simple build system for Mono?
#
LIBS=-r:System.Windows.Forms -r:System.Drawing
RES=-resource:words.txt
SRCS=Qz.cs

CSC=gmcs $(LIBS) $(RES) $(SRCS)

Qz.exe: $(SRCS) words.txt
	$(CSC) -o+ -t:winexe

.PHONY: debug clean all

debug: words.txt
	$(CSC) -debug+

clean:
	rm -f Qz.exe

all: clean Qz.exe
