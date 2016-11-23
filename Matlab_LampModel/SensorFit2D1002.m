dist=[0.001	5.08	10.16	15.24	20.32	25.4	30.48	35.56	40.64	45.72	50.8];
bri=[6726.16	5701.66	4845.5	3907.15	3381.05	2786.98	2466.81	2410.24	2044.6	2166.34	2204.87];
std=[30.71857267	62.9105859	56.87475164	38.57810522	25.57278185	17.08622222	13.34975882	12.33629776	13.21844441	13.5529042	14.13474478
];

% quad fit
SensorFit2D1002Quad(dist,bri,std);
% Linear model Poly2:
%        f(x) = p1*x^2 + p2*x + p3
% Coefficients (with 95% confidence bounds):
%        p1 =       2.524  (2.253, 2.794)
%        p2 =        -216  (-230.3, -201.7)
%        p3 =        6720  (6564, 6876)
% 
% Goodness of fit:
%   SSE: 6.297e+004
%   R-square: 0.9975
%   Adjusted R-square: 0.9969
%   RMSE: 88.72

% robust quad fit
SensorFit2D1002QuadRobust(dist,bri);
% Linear model Poly2:
%        f(x) = p1*x^2 + p2*x + p3
% Coefficients (with 95% confidence bounds):
%        p1 =       2.538  (2.269, 2.808)
%        p2 =        -217  (-231.2, -202.8)
%        p3 =        6727  (6572, 6882)
% 
% Goodness of fit:
%   SSE: 6.26e+004
%   R-square: 0.9976
%   Adjusted R-square: 0.9969
%   RMSE: 88.46


% exp. fit
SensorFit2D1002Exp(dist,bri,std);
% General model Exp1:
%        f(x) = a*exp(b*x)
% Coefficients (with 95% confidence bounds):
%        a =        6463  (5910, 7015)
%        b =    -0.02825  (-0.03295, -0.02354)
% 
% Goodness of fit:
%   SSE: 1.037e+006
%   R-square: 0.9595
%   Adjusted R-square: 0.955
%   RMSE: 339.4

% robust power
SensorFit2D1002PowRobust(dist,bri,std)
%General model Power2:
%        f(x) = a*x^b+c
% Coefficients (with 95% confidence bounds):
%        a =  2.027e+004  (-1.501e+004, 5.556e+004)
%        b =     -0.1153  (-1.298, 1.067)
%        c = -1.098e+004  (-4.226e+004, 2.03e+004)
% 
% Goodness of fit:
%   SSE: 1.5e+007
%   R-square: 0.4137
%   Adjusted R-square: 0.2671
%   RMSE: 1369

% power
SensorFit2D1002Pow(dist,bri,std)
% General model Power2:
%        f(x) = a*x^b+c
% Coefficients (with 95% confidence bounds):
%        a =        -763  (-1409, -116.8)
%        b =      0.4896  (0.2936, 0.6856)
%        c =        6908  (6036, 7780)
% 
% Goodness of fit:
%   SSE: 1.069e+006
%   R-square: 0.9582
%   Adjusted R-square: 0.9477
%   RMSE: 365.6


% Linear model Poly2:
%        f(x) = p1*x^2 + p2*x + p3
% Coefficients (with 95% confidence bounds):
%        p1 =     0.04577  (0.01898, 0.07255)
%        p2 =      -3.626  (-5.039, -2.214)
%        p3 =        82.4  (66.97, 97.82)
% 
% Goodness of fit:
%   SSE: 616.6
%   R-square: 0.8169
%   Adjusted R-square: 0.7711
%   RMSE: 8.779
