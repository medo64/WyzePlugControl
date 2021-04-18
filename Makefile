.PHONY: all clean debug release distclean dist install uninstall

ifeq ($(PREFIX),)
    PREFIX := /usr/local/
endif


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


install: release
	@sudo install -d $(DESTDIR)/$(PREFIX)/bin/
	@sudo install bin/WyzePlugControl $(DESTDIR)/$(PREFIX)/bin/
	@echo Installed at $(DESTDIR)/$(PREFIX)/bin/ | sed 's^//^/^g'

uninstall: $(DESTDIR)/$(PREFIX)/bin/wyzeplugcontrol
	@sudo $(RM) $(DESTDIR)/$(PREFIX)/bin/wyzeplugcontrol
