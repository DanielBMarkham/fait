```shell

cat appsample.txt | ./app
cat appsample.txt | ./app-test

./app
./app-test

cat appsample.txt | ./app | ./app-test
cat appsample.txt | ./app-test | ./app

cat appsample.txt | ./app --v INFO --h
cat appsample.txt | ./app-test --v INFO --h


cat appsample.txt | ./app --v INFO --h | ./app-test --v INFO --h
cat appsample.txt | ./app-test --v INFO --h | ./app --v INFO --h


```

```dos

type appsample.txt | app
type appsample.txt | app-test

app
app-test

type appsample.txt | app | app-test
type appsample.txt | app-test | app

type appsample.txt | app --v INFO --h
type appsample.txt | app-test --v INFO --h


type appsample.txt | app --v INFO --h | app-test --v INFO --h
type appsample.txt | app-test --v INFO --h | app --v INFO --h


```