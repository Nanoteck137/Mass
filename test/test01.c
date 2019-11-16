#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	int a = 123;
	int b = -123;
	int c = b - a;

	printf("C: %d\n", c);

	return 0;
}