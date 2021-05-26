
# TLS Tester

## Running a single test against a host

If you add the `test` command to the command line you can trigger a 
standalone test run.

Use `--hostnames` to specify the hosts to test and `--tests` to restrict 
testing to specific test numbers

Example: Run whole test suite against a host
```
dotnet MailCheck.Mx.TlsTester.dll test --hostnames "mx1.domain.com"
```

Example: Run one test  against a host
```
dotnet MailCheck.Mx.TlsTester.dll test --hostnames "mx1.domain.com" --tests 11
```
