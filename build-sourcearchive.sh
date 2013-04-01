#!/bin/sh
EXPORTDIR=dist/serpent-export

if [ -d ${EXPORTDIR} ]; then
	rm -r ${EXPORTDIR}
fi
svn export . ${EXPORTDIR}
tar czf dist/serpent-src-py-java-net.tar.gz -C dist serpent-export
rm -r ${EXPORTDIR}
