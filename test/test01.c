#include <stdio.h>

void Test() {
    printf("Hello World\n");
}

int main(int argc, char **argv)
{
    unsigned int x = 16;
    unsigned int y = x % 3 * 2 + 1;

    return 0;
}