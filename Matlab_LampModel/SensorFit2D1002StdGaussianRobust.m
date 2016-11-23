function SensorFit2D1002StdGaussianRobust(dist,std)
%SENSORFIT2D1002STDGAUSSIANROBUST    Create plot of datasets and fits
%   SENSORFIT2D1002STDGAUSSIANROBUST(DIST,STD)
%   Creates a plot, similar to the plot in the main curve fitting
%   window, using the data that you provide as input.  You can
%   apply this function to the same data you used with cftool
%   or with different data.  You may want to edit the function to
%   customize the code and this help message.
%
%   Number of datasets:  1
%   Number of fits:  1

 
% Data from dataset "std vs. dist":
%    X = dist:
%    Y = std:
%    Unweighted
%
% This function was automatically generated on 02-Oct-2006 12:16:42

% Set up figure to receive datasets and fits
f_ = clf;
figure(f_);
set(f_,'Units','Pixels','Position',[373 126 680 477]);
legh_ = []; legt_ = {};   % handles and text for legend
xlim_ = [Inf -Inf];       % limits of x axis
ax_ = axes;
set(ax_,'Units','normalized','OuterPosition',[0 0 1 1]);
set(ax_,'Box','on');
axes(ax_); hold on;

 
% --- Plot data originally in dataset "std vs. dist"
dist = dist(:);
std = std(:);
h_ = line(dist,std,'Parent',ax_,'Color',[0.333333 0 0.666667],...
     'LineStyle','none', 'LineWidth',1,...
     'Marker','.', 'MarkerSize',12);
xlim_(1) = min(xlim_(1),min(dist));
xlim_(2) = max(xlim_(2),max(dist));
legh_(end+1) = h_;
legt_{end+1} = 'std vs. dist';

% Nudge axis limits beyond data limits
if all(isfinite(xlim_))
   xlim_ = xlim_ + [-1 1] * 0.01 * diff(xlim_);
   set(ax_,'XLim',xlim_)
end


% --- Create fit "fit 2"
fo_ = fitoptions('method','NonlinearLeastSquares','Robust','On','Lower',[-Inf  -Inf 0 ],'MaxFunEvals',231,'MaxIter',279);
ok_ = ~(isnan(dist) | isnan(std));
st_ = [62.9105859 5.08 10.66757152519 ];
set(fo_,'Startpoint',st_);
ft_ = fittype('gauss1');

% Fit this model using new data
cf_ = fit(dist(ok_),std(ok_),ft_,fo_);

% Or use coefficients from the original fit:
if 0
   cv_ = {7.751544952281e+262, -25822.10444778, 1053.318112013};
   cf_ = cfit(ft_,cv_{:});
end

% Plot this fit
h_ = plot(cf_,'fit',0.95);
legend off;  % turn off legend from plot method call
set(h_(1),'Color',[1 0 0],...
     'LineStyle','-', 'LineWidth',2,...
     'Marker','none', 'MarkerSize',6);
legh_(end+1) = h_(1);
legt_{end+1} = 'fit 2';

% Done plotting data and fits.  Now finish up loose ends.
hold off;
h_ = legend(ax_,legh_,legt_,'Location','NorthEast');  
set(h_,'Interpreter','none');
xlabel(ax_,'');               % remove x label
ylabel(ax_,'');               % remove y label
