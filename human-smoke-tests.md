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

type test.txt | part2
type test.txt | part2-test

part2
part2-test


```