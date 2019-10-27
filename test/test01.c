#include <stdio.h>

void Test()
{
    printf("Hello World\n");
}

int main(int argc, char **argv)
{
    int x = 0;
    while (x < 4)
    {
        printf("Test\n");
        x++;
    }

    return 0;
}