Making a release to Maven Central (Central Publishing Portal):


First make sure that JAVA_HOME is set (needed for the javadoc tool)
For example:  export JAVA_HOME=/usr/lib/jvm/java-21-openjdk/


Ensure your ~/.m2/settings.xml has a <server> entry for 'central' with your User Token.

$ mvn clean deploy -Prelease


Monitor progress and finalise via https://central.sonatype.com


See also:
https://central.sonatype.com
https://central.sonatype.org/publish/publish-portal/
