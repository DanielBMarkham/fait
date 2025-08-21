```shell

cat sample.txt | ./app
cat sample.txt | ./app-test

./app
./app-test

cat sample.txt | ./app | ./app-test
cat sample.txt | ./app-test | ./app

cat sample.txt | ./app --v INFO --h
cat sample.txt | ./app-test --v INFO --h


cat sample.txt | ./app --v INFO --h | ./app-test --v INFO --h
cat sample.txt | ./app-test --v INFO --h | ./app --v INFO --h


```

```dos

type sample.txt | app
type sample.txt | app-test

app
app-test

type sample.txt | app | app-test
type sample.txt | app-test | app

type sample.txt | app --v INFO --h
type sample.txt | app-test --v INFO --h


type sample.txt | app --v INFO --h | app-test --v INFO --h
type sample.txt | app-test --v INFO --h | app --v INFO --h


```