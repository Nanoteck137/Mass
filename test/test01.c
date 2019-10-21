#include <stdio.h>

const int x = 123;
const int y = 321;
int z = x + y;
// static int y = 321;

struct Hello
{
    int x;
    union {
        int integer;
        float floating;
    };
};

int main(int argc, char **argv)
{
    int local = 123;
    /*struct Hello hello;
    hello.x = x;
    hello.floating = 3.14;
    printf("Hello: x = %d, int = %d, float = %f", hello.x, hello.integer, hello.floating);*/
    // printf("Hello World %d %d\n", x, y);
    return 0;
}