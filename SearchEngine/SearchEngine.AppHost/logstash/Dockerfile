FROM logstash:9.0.1

USER root

ENV JDBC_DRIVER_LIBRARY=/usr/share/logstash/drivers/mssql-jdbc.jar
ENV XPACK_MONITORING_ENABLED=false

RUN logstash-plugin install logstash-output-opensearch

COPY --chown=1000:0 config/*.yml /usr/share/logstash/config/
COPY --chown=1000:0 drivers/mssql-jdbc-13.6.0.jre11.jar /usr/share/logstash/drivers/mssql-jdbc.jar
COPY --chown=1000:0 pipelines/*.conf /usr/share/logstash/pipeline/

RUN chmod 0644 /usr/share/logstash/config/*.yml /usr/share/logstash/pipeline/*.conf /usr/share/logstash/drivers/*.jar

USER 1000
