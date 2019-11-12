#include <stdio.h>

int main(int argc, char **argv)
{
	int a[4] = {1, 2, 3, 4};

	unsigned int b = 5;
	unsigned int c = 1;
	unsigned int d = b + c;
	unsigned int *e = &d;
	return 0;
}