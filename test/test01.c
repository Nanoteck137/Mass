#include <stdio.h>

int main(int argc, char **argv)
{
	int a[2][2] = {{1, 2}, {3, 4}};

	int b = a[0][0];
	printf("%c\n", argv[0][0]);
	return 0;
}