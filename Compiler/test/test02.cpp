#include <stdio.h>

namespace Test {
    int add(int a, int b) {
        return a + b;
    }
}

int main(int argc, char **argv)
{
    int res = Test::add(5, 10);
    return 0;
}