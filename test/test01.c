#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	int a[4] = {1, 2, 3, 4};
	int *ptr = a;

	printf("Value: %d\n", *(ptr + 1));
	return 0;
}