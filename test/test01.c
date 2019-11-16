#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	int a[4] = {1, 2, 3, 4};
	int *ptr = a;
	int index = 2;

	printf("Value: %d\n", *(ptr + index));
	return 0;
}