.PHONY: all clean debug release distclean dist

all: release


clean:
	@./Make.sh clean

debug:
	@./Make.sh debug

release:
	@./Make.sh release


distclean:
	@./Make.sh distclean

dist:
	@./Make.sh dist
