#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	int a[4] = {1, 2, 3, 4};
	int *ptr = a;
	int index = 2;

	int* testPtr = ptr + index;

	printf("Value: %d\n", *(testPtr - 2));
	return 0;
}