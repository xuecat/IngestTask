FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.0
WORKDIR /opt/ingesttasksvr
EXPOSE 9041
COPY  publish .
CMD []
ENTRYPOINT ["/bin/bash", "-c", "/run.sh"]



