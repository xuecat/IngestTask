FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.2
WORKDIR /opt/ingesttasksvr
COPY  publish .
CMD []
ENTRYPOINT ["/bin/bash", "-c", "/run.sh"]



