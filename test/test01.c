#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	uint32_t a = 4;
	
	do {
		printf("A: %d\n", a);
		a--;
	} while(a > 0);
	
	return 0;
}