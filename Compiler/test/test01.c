#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>

void test() {
	test();
}

int main(int argc, char **argv)
{
	test();
	return 0;
}