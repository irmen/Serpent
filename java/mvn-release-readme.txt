Making a release to Sonatype Nexus/maven central:


$ mvn release:clean release:prepare release:perform


Requires version number in the pom.xml to be "x.y-SNAPSHOT".
