#include <stdio.h>

const int A = 123;
int b = 321 + A;

int x = 321;
int y = 123;
int z = x + y;

typedef int (*TestPtr)(int x, int y);

int add(int x, int y)
{
    return x + y;
}

int sub(int x, int y)
{
    return x + y;
}

int main(int argc, char **argv)
{
    TestPtr func = add;
    func(123, 321);

    return 0;
}