# Is there a good, simple build system for Mono?
#
LIBS=-r:System.Windows.Forms -r:System.Drawing
RES=-resource:words.resources
SRCS=Qz.cs

CSC=gmcs $(LIBS) $(RES) $(SRCS)

Qz.exe: $(SRCS) words.resources
	$(CSC) -o+ -t:winexe

words.resources: words.txt
	resgen2 -compile words.txt


.PHONY: debug clean all

debug: words.resources
	$(CSC) -debug+

clean:
	rm -f Qz.exe words.resources

all: clean Qz.exe
