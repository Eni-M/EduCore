# Load tests

`school-day.js` is a k6 script: anonymous traffic on the public pages, and office staff working
through the dashboard, the bell, the student and guardian lists and the statistics overview. It
fails if more than 1% of requests fail, or the 95th percentile exceeds 500 ms (public) or
800 ms (signed in).

## Running it

k6 runs from its container, so nothing needs installing:

```bash
docker run --rm -i -v "$PWD/loadtests:/scripts" grafana/k6:1.3.0 run /scripts/school-day.js \
  -e BASE_URL=https://host.docker.internal:7132 -e TOKEN=<session token> -e INSECURE=1
```

- `TOKEN` is a signed-in office user's session cookie value (the JWT). Without it only the public
  scenario runs.
- `SCALE` multiplies the traffic (default 1: 5 public requests a second, 10 staff).
- `INSECURE=1` accepts the local development certificate. Never use it against a real deployment.

## The instance under test

The API limits each user to 100 requests a minute, by design. A load test with one account
measures that limiter, not the API. Start the instance under test with the limit raised:

```bash
RateLimiting__Global__PermitsPerPeriod=100000 dotnet run --launch-profile https
```

Run against a staging copy, never production: the signed-in scenario reads real records, and
every read of a student list is audited.
