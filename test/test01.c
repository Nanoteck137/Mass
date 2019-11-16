#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	for(int i = 0; i < 10; i++) {
		if(i == 2)
			continue;
		printf("I: %d\n", i);
	}
	return 0;
}